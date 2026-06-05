using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

namespace AU_TheDirectorsCut
{
    public static class DirectorCore
    {
        public static byte? DirectorPlayerId { get; private set; }
        public static string? DirectorName { get; private set; }
        public static bool IsCutActive { get; private set; }
        public static bool PendingAutoGG = false;
        private static float pendingAutoGGDelay = 0f;
        private static float _snapshotTimer = 0f;

        private static int cutStep;
        private static float cutStepTimer;
        private static readonly Dictionary<byte, Vector2> cutStartPositions = new();

        private static readonly Dictionary<string, float> _cd = new();
        private static readonly Dictionary<string, float> _cdMax = new()
        {
            ["/cut"] = 30f,
            ["/swap"] = 15f,
            ["/blind"] = 25f,
            ["/darkness"] = 35f,
            ["/freeze"] = 30f,
            ["/spin"] = 20f,
            ["/randomcolors"] = 20f,
            ["/shuffle"] = 25f,
            ["/teleportall"] = 20f,
        };

        private static readonly Dictionary<byte, (Vector2 pos, float rem)> _frozen = new();
        private static readonly Dictionary<byte, (Vector2 center, float angle, float rem)> _spin = new();
        private static float _visionDur;
        // Cadence des téléportations d'effet (freeze/spin) : le RPC SnapTo est
        // diffusé à tous, donc on le limite à ~10/s pour ne pas saturer le réseau
        // et éviter un kick anti-triche côté client vanilla.
        private static float _effectTpTimer;

        // Données de la partie précédente
        public static IReadOnlyList<string> LastAlive => _lastAlive;
        public static IReadOnlyList<string> LastDead => _lastDead;
        private static List<string> _lastAlive = new();
        private static List<string> _lastDead = new();

        public static void SnapshotEndState() => SnapshotEndState(true);

        public static void SnapshotEndState(bool verbose)
        {
            var allPlayers = PlayerControl.AllPlayerControls.ToArray();

            var alive = allPlayers
                .Where(p => p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected)
                .Select(p => p.Data.PlayerName).ToList();
            var dead = allPlayers
                .Where(p => p?.Data != null && p.Data.IsDead && !p.Data.Disconnected)
                .Select(p => p.Data.PlayerName).ToList();

            // Ne JAMAIS écraser un snapshot valide par un vide : à la fin de
            // partie, les PlayerControl peuvent déjà être détruits selon l'ordre
            // des événements (EndGame / ExitGame / ShipStatus.OnDestroy). Dans ce
            // cas on conserve le dernier état connu capturé en jeu.
            if (alive.Count == 0 && dead.Count == 0)
            {
                if (verbose)
                    Plugin.Log?.LogInfo("[DirectorCore] Snapshot ignoré (aucun joueur présent, on garde le précédent).");
                return;
            }

            _lastAlive = alive;
            _lastDead = dead;
            if (verbose)
                Plugin.Log?.LogInfo($"[DirectorCore] Snapshot — Alive:{_lastAlive.Count} Dead:{_lastDead.Count}");
        }

        public static float CooldownRemaining(string cmd) => _cd.TryGetValue(cmd, out float r) ? r : 0f;
        public static bool IsOnCooldown(string cmd) => _cd.TryGetValue(cmd, out float r) && r > 0f;

        public static void Initialize() { Reset(); Plugin.Log?.LogInfo("[DirectorCore] Initialisé."); }

        public static void Reset()
        {
            DirectorPlayerId = null;
            DirectorName = null;
            IsCutActive = false;
            PendingAutoGG = false;
            pendingAutoGGDelay = 0f;
            cutStep = 0; cutStepTimer = 0f;
            cutStartPositions.Clear();
            _frozen.Clear(); _spin.Clear();
            _visionDur = 0f;
            _cd.Clear();
            NetworkManager.ResetGlobalVision();
            ChatManager.ClearWelcomeSent();
        }

        public static void OnPlayerDie(PlayerControl player)
        {
            // Hôte uniquement, et on n'attribue qu'UNE fois (le tout premier mort).
            if (AmongUsClient.Instance?.AmHost != true) return;
            if (player?.Data == null) return;
            if (DirectorPlayerId.HasValue) return;

            DirectorPlayerId = player.PlayerId;
            DirectorName     = player.Data.PlayerName;

            // OwnerId = client réseau propriétaire du PlayerControl.
            // HostId  = client hôte du lobby. L'égalité identifie l'avatar de l'hôte,
            // sinon c'est un client distant (vanilla / mobile).
            bool isHost = player.OwnerId == AmongUsClient.Instance.HostId;

            Plugin.Log?.LogInfo(
                $"[Director] RÉALISATEUR attribué → \"{DirectorName}\" " +
                $"(PlayerId={player.PlayerId}, OwnerId={player.OwnerId}, " +
                $"{(isHost ? "HÔTE du lobby" : "CLIENT vanilla/mobile")}).");

            SendHostMessage(
                string.Format(ModMessages.FirstDirector,      player.Data.PlayerName),
                string.Format(ModMessages.FirstDirectorPlain, player.Data.PlayerName));
        }

        public static bool IsDirector(byte id) => DirectorPlayerId.HasValue && DirectorPlayerId.Value == id;

        private static string NumberToFrench(int number)
        {
            return number.ToString();
        }

        private static bool TryParseId(string input, out byte id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            char c = char.ToUpperInvariant(input.Trim()[0]);
            if (c >= 'A' && c <= 'Z')
            {
                id = (byte)(c - 'A');
                return true;
            }
            // Also accept numbers for backwards compatibility
            return byte.TryParse(input, out id);
        }

        private static bool TryCooldown(string cmd)
        {
            if (IsOnCooldown(cmd))
            {
                int remaining = Mathf.CeilToInt(CooldownRemaining(cmd));
                string remainingStr = NumberToFrench(remaining);
                SendHostMessage(string.Format(ModMessages.CooldownMsg, cmd, remainingStr));
                return false;
            }
            if (_cdMax.TryGetValue(cmd, out float max)) _cd[cmd] = max;
            return true;
        }

        public static bool TryProcessCommand(PlayerControl sender, string raw)
        {
            if (!AmongUsClient.Instance.AmHost || sender == null) return false;
            string msg = raw.Trim();
            if (!msg.StartsWith("/")) return false;
            var parts = msg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            string cmd = parts[0].ToLowerInvariant();

            bool inLobby = ShipStatus.Instance == null;

            // ────────────────────────────────────────────────
            // COMMANDES DEV — /start /stop /setdirector /setimpostor
            // Utilisables UNIQUEMENT si devMode == true.
            // Hors devMode : aucun message « désactivé », elles sont
            // simplement traitées comme une commande inconnue.
            // Réservées à l'hôte (avatar local).
            // ────────────────────────────────────────────────
            bool isDevCommand = cmd == "/start" || cmd == "/stop"
                             || cmd == "/setdirector";

            if (isDevCommand)
            {
                if (!DevModeManager.devMode)
                {
                    // devMode OFF → commande non reconnue.
                    SendHostMessage($"Commande inconnue : {cmd} — /help");
                    return true;
                }

                // Seul l'hôte peut déclencher ces commandes de contrôle.
                if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                {
                    SendHostMessage(ModMessages.HostOnly, ModMessages.HostOnlyPlain);
                    return true;
                }

                switch (cmd)
                {
                    case "/start":
                        if (!inLobby) { SendHostMessage("Pas en lobby !"); return true; }
                        AmongUsClient.Instance.StartGame();
                        return true;

                    case "/stop":
                        if (inLobby)
                        {
                            SendHostMessage(ModMessages.NoGameRunning, ModMessages.NoGameRunningPlain);
                            return true;
                        }
                        try
                        {
                            // RpcEndGame est indépendant de CheckEndCriteria : il
                            // termine la partie même avec devMode actif. On désactive
                            // d'abord GameManager pour qu'il ne ré-évalue pas la fin
                            // (même approche que TOHE pour une fin forcée). La raison
                            // « disconnect » évite tout calcul de vainqueur.
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                            SendHostMessage(ModMessages.GameStopped, ModMessages.GameStoppedPlain);
                            Plugin.Log?.LogInfo("[Director] /stop → RpcEndGame.");
                        }
                        catch (Exception e) { Plugin.Log?.LogError($"[/stop] {e.Message}"); }
                        return true;

                    case "/setdirector":
                    // Avec un ID → ce joueur devient Réalisateur.
                    // Sans ID → l'hôte se désigne lui-même (comportement d'origine).
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte did))
                    {
                        var dtarget = FindById(did);
                        if (dtarget?.Data == null)
                        {
                            SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        DirectorPlayerId = dtarget.PlayerId;
                        DirectorName = dtarget.Data.PlayerName;
                        SendHostMessage(
                            string.Format(ModMessages.DirectorSet, dtarget.Data.PlayerName),
                            string.Format(ModMessages.DirectorSetPlain, dtarget.Data.PlayerName));
                    }
                    else
                    {
                        DirectorPlayerId = sender.PlayerId;
                        DirectorName = sender.Data.PlayerName;
                        SendHostMessage(
                            string.Format(ModMessages.DirectorSet, sender.Data.PlayerName),
                            string.Format(ModMessages.DirectorSetPlain, sender.Data.PlayerName));
                    }
                    return true;
                }
            }

            switch (cmd)
            {
                case "/welcome":
                    ChatManager.QueueSlow(ModMessages.Welcome, ModMessages.WelcomePlain);
                    return true;

                case "/help":
                    ChatManager.QueueSlow(ModMessages.Help1, ModMessages.Help1Plain);
                    ChatManager.QueueSlow(ModMessages.Help2, ModMessages.Help2Plain);
                    ChatManager.QueueSlow(ModMessages.Help3, ModMessages.Help3Plain);
                    return true;

                case "/gg":
                    ChatManager.SendPrivateGGToAll();
                    return true;

                case "/discord":
                    ChatManager.QueueSlow(ModMessages.Discord, ModMessages.DiscordPlain);
                    return true;

                case "/players":
                {
                    var players = PlayerControl.AllPlayerControls.ToArray()
                        .Where(p => p?.Data != null)
                        .OrderBy(p => p.PlayerId)
                        .ToList();

                    const int maxLen = 100;      // limite DURE vanilla
                    const int maxMessages = 8;   // garde-fou : tronque au-delà

                    var chunksPlain = new List<string>();
                    var chunksColored = new List<string>();

                    string plainCur = "Joueurs : ";
                    string coloredCur = "Joueurs : ";
                    int partsInCur = 0;
                    bool truncated = false;

                    foreach (var p in players)
                    {
                        string coloredPart = $"{p.PlayerId} {p.Data.PlayerName}{(p.Data.IsDead ? " <color=#ff6b6b>(éliminé)</color>" : "")}";
                        string plainPart = $"{p.PlayerId} {p.Data.PlayerName}{(p.Data.IsDead ? " (éliminé)" : "")}";
                        string sep = partsInCur > 0 ? " | " : "";

                        // On remplit le message courant tant qu'on reste <= 100 caractères
                        if ((plainCur + sep + plainPart).Length <= maxLen)
                        {
                            plainCur += sep + plainPart;
                            coloredCur += sep + coloredPart;
                            partsInCur++;
                        }
                        else
                        {
                            // message plein -> on le valide et on repart sur une nouvelle page
                            if (partsInCur > 0)
                            {
                                chunksPlain.Add(plainCur);
                                chunksColored.Add(coloredCur);
                            }
                            if (chunksPlain.Count >= maxMessages) { truncated = true; break; }
                            plainCur = plainPart;     // nouvelle page, sans préfixe
                            coloredCur = coloredPart;
                            partsInCur = 1;
                        }
                    }

                    if (!truncated && partsInCur > 0)
                    {
                        chunksPlain.Add(plainCur);
                        chunksColored.Add(coloredCur);
                    }
                    else if (truncated && chunksPlain.Count > 0)
                    {
                        chunksPlain[chunksPlain.Count - 1] += " ...";
                        chunksColored[chunksColored.Count - 1] += " ...";
                    }

                    // Chaque page : envoi LENT (3.5s entre chaque), borné à 100 car. à l'émission.
                    for (int i = 0; i < chunksPlain.Count; i++)
                        ChatManager.QueueSlow(chunksColored[i], chunksPlain[i]);

                    return true;
                }

                case "/hcut":
                    ChatManager.Queue(ModMessages.HelpCut, ModMessages.HelpCutPlain);
                    return true;
                case "/hswap":
                    ChatManager.Queue(ModMessages.HelpSwap, ModMessages.HelpSwapPlain);
                    return true;
                case "/hblind":
                    ChatManager.Queue(ModMessages.HelpBlind, ModMessages.HelpBlindPlain);
                    return true;
                case "/hdarkness":
                    ChatManager.Queue(ModMessages.HelpDarkness, ModMessages.HelpDarknessPlain);
                    return true;
                case "/hfreeze":
                    ChatManager.Queue(ModMessages.HelpFreeze, ModMessages.HelpFreezePlain);
                    return true;
                case "/hspin":
                    ChatManager.Queue(ModMessages.HelpSpin, ModMessages.HelpSpinPlain);
                    return true;
                case "/hrandomcolors":
                    ChatManager.Queue(ModMessages.HelpRandomColors, ModMessages.HelpRandomColorsPlain);
                    return true;
                case "/hshuffle":
                    ChatManager.Queue(ModMessages.HelpShuffle, ModMessages.HelpShufflePlain);
                    return true;
                case "/hteleportall":
                    ChatManager.Queue(ModMessages.HelpTeleportAll, ModMessages.HelpTeleportAllPlain);
                    return true;
            }

            if (inLobby)
            {
                Plugin.Log?.LogInfo($"[DirectorCore] {cmd} ignoré en lobby.");
                return true;
            }

            if (!IsDirector(sender.PlayerId))
            {
                SendHostMessage(string.Format(ModMessages.NotDirector, sender.Data.PlayerName), string.Format(ModMessages.NotDirectorPlain, sender.Data.PlayerName));
                return true;
            }

            switch (cmd)
            {
                case "/cut":
                    if (!TryCooldown("/cut")) return true;
                    StartCut();
                    return true;

                case "/swap":
                    if (parts.Length >= 3 && byte.TryParse(parts[1], out byte a) && byte.TryParse(parts[2], out byte b))
                    {
                        if (!TryCooldown("/swap")) return true;
                        var p1 = FindById(a); var p2 = FindById(b);
                        if (p1 == null || p2 == null)
                        {
                            SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        SendHostMessage(string.Format(ModMessages.SwapSuccess, p1.Data.PlayerName, p2.Data.PlayerName), string.Format(ModMessages.SwapSuccessPlain, p1.Data.PlayerName, p2.Data.PlayerName));
                        NetworkManager.SwapPlayers(p1, p2);
                    }
                    else
                        SendHostMessage(ModMessages.UsageSwap, ModMessages.UsageSwapPlain);
                    return true;

                case "/blind":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte bid))
                    {
                        if (!TryCooldown("/blind")) return true;
                        var bt = FindById(bid);
                        if (bt == null)
                        {
                            SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        SendHostMessage(string.Format(ModMessages.BlindSuccess, bt.Data.PlayerName), string.Format(ModMessages.BlindSuccessPlain, bt.Data.PlayerName));
                        NetworkManager.SetGlobalVision(0.05f);
                        _visionDur = 8f;
                    }
                    else
                        SendHostMessage(ModMessages.UsageBlind, ModMessages.UsageBlindPlain);
                    return true;

                case "/darkness":
                    if (!TryCooldown("/darkness")) return true;
                    SendHostMessage(ModMessages.HelpDarkness, ModMessages.HelpDarknessPlain);
                    NetworkManager.SetGlobalVision(0.05f);
                    _visionDur = 10f;
                    return true;

                case "/freeze":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte fid))
                    {
                        if (!TryCooldown("/freeze")) return true;
                        var ft = FindById(fid);
                        if (ft == null || ft.Data.IsDead)
                        {
                            SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        _frozen[ft.PlayerId] = (ft.GetTruePosition(), 8f);
                        SendHostMessage(string.Format(ModMessages.FreezeSuccess, ft.Data.PlayerName), string.Format(ModMessages.FreezeSuccessPlain, ft.Data.PlayerName));
                    }
                    else
                        SendHostMessage(ModMessages.UsageFreeze, ModMessages.UsageFreezePlain);
                    return true;

                case "/spin":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte sid))
                    {
                        if (!TryCooldown("/spin")) return true;
                        var st = FindById(sid);
                        if (st == null || st.Data.IsDead)
                        {
                            SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        _spin[st.PlayerId] = (st.GetTruePosition(), 0f, 5f);
                        SendHostMessage(string.Format(ModMessages.SpinSuccess, st.Data.PlayerName), string.Format(ModMessages.SpinSuccessPlain, st.Data.PlayerName));
                    }
                    else
                        SendHostMessage(ModMessages.UsageSpin, ModMessages.UsageSpinPlain);
                    return true;

                case "/randomcolors":
                    if (!TryCooldown("/randomcolors")) return true;
                    SendHostMessage(ModMessages.RandomColorsStart, ModMessages.RandomColorsStartPlain);
                    NetworkManager.RandomizeColors();
                    return true;

                case "/shuffle":
                    if (!TryCooldown("/shuffle")) return true;
                    SendHostMessage(ModMessages.ShuffleStart, ModMessages.ShuffleStartPlain);
                    NetworkManager.ShuffleAllPlayers();
                    return true;

                case "/teleportall":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte tid))
                    {
                        if (!TryCooldown("/teleportall")) return true;
                        var tt = FindById(tid);
                        if (tt == null)
                        {
                            SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        SendHostMessage(string.Format(ModMessages.TeleportAllStart, tt.Data.PlayerName), string.Format(ModMessages.TeleportAllStartPlain, tt.Data.PlayerName));
                        NetworkManager.TeleportAllTo(tt);
                    }
                    else
                        SendHostMessage(ModMessages.UsageTeleportAll, ModMessages.UsageTeleportAllPlain);
                    return true;

                default:
                    SendHostMessage($"Commande inconnue : {cmd} — /help");
                    return true;
            }
        }

        private static void StartCut()
        {
            if (IsCutActive) return;
            IsCutActive = true; cutStep = 1; cutStepTimer = 2f;
            cutStartPositions.Clear();
            NetworkManager.SendCutSignal();
        }

        private static void AdvanceCutStep()
        {
            switch (cutStep)
            {
                case 1:
                    NetworkManager.StopCutSignal();
                    foreach (var p in NetworkManager.Alive())
                        cutStartPositions[p.PlayerId] = p.GetTruePosition();
                    SendHostMessage(ModMessages.CutStart, ModMessages.CutStartPlain);
                    cutStep = 2; cutStepTimer = 5f;
                    break;
                case 2:
                    NetworkManager.SendCutSignal();
                    cutStep = 3; cutStepTimer = 2f;
                    break;
                case 3:
                    NetworkManager.StopCutSignal();
                    SendHostMessage(ModMessages.Sun, ModMessages.SunPlain);
                    IsCutActive = false; cutStep = 0;
                    break;
            }
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float dt = Time.deltaTime;

            // Capture continue de l'état de la partie tant qu'on est en jeu.
            // Ainsi, quel que soit l'ordre de destruction des joueurs à la fin,
            // on dispose toujours d'un état récent pour le récap /gg.
            if (ShipStatus.Instance != null)
            {
                _snapshotTimer -= dt;
                if (_snapshotTimer <= 0f)
                {
                    SnapshotEndState(false);
                    _snapshotTimer = 1f;
                }
            }

            // Fin de partie : le récap /gg est désormais émis par le flux welcome
            // (étape 1, juste APRÈS le message de bienvenue, joueur par joueur).
            // Ici on se contente de retomber le drapeau une fois en lobby, pour
            // éviter tout double envoi.
            if (PendingAutoGG)
            {
                if (ShipStatus.Instance == null)
                {
                    PendingAutoGG = false;
                    pendingAutoGGDelay = 0f;
                    Plugin.Log?.LogInfo("[DirectorCore] Fin de partie → récap délégué au flux welcome.");
                }
            }

            foreach (var k in _cd.Keys.ToList())
                _cd[k] = Mathf.Max(0f, _cd[k] - dt);

            if (IsCutActive)
            {
                cutStepTimer -= dt;
                if (cutStepTimer <= 0f) AdvanceCutStep();
                if (cutStep == 2)
                {
                    foreach (var p in NetworkManager.Alive())
                    {
                        if (!cutStartPositions.TryGetValue(p.PlayerId, out Vector2 start)) continue;
                        if (Vector2.Distance(start, p.GetTruePosition()) > 0.1f)
                        {
                            SendHostMessage(string.Format(ModMessages.PlayerCaught, p.Data.PlayerName), string.Format(ModMessages.PlayerCaughtPlain, p.Data.PlayerName));
                            if (DirectorOptions.CutKills) NetworkManager.MurderPlayer(p);
                            cutStartPositions.Remove(p.PlayerId);
                        }
                    }
                }
            }

            if (_visionDur > 0f)
            {
                _visionDur -= dt;
                if (_visionDur <= 0f)
                {
                    NetworkManager.ResetGlobalVision();
                    SendHostMessage("Vision restaurée.");
                }
            }

            // Tick de téléportation d'effet (~10 Hz) partagé par freeze et spin.
            _effectTpTimer -= dt;
            bool tpTick = _effectTpTimer <= 0f;
            if (tpTick) _effectTpTimer = 0.1f;

            foreach (var k in _frozen.Keys.ToList())
            {
                var (pos, rem) = _frozen[k];
                var p = FindById(k);
                if (p == null || p.Data.IsDead) { _frozen.Remove(k); continue; }
                if (tpTick && Vector2.Distance(p.GetTruePosition(), pos) > 0.15f) NetworkManager.Teleport(p, pos);
                float nr = rem - dt;
                if (nr <= 0f)
                {
                    _frozen.Remove(k);
                    SendHostMessage(string.Format(ModMessages.FreezeEnd, p.Data.PlayerName), string.Format(ModMessages.FreezeEndPlain, p.Data.PlayerName));
                }
                else
                    _frozen[k] = (pos, nr);
            }

            foreach (var k in _spin.Keys.ToList())
            {
                var (center, angle, rem) = _spin[k];
                var p = FindById(k);
                if (p == null || p.Data.IsDead) { _spin.Remove(k); continue; }
                angle += 3.5f * dt;
                if (tpTick) NetworkManager.Teleport(p, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.0f);
                float nr = rem - dt;
                if (nr <= 0f)
                {
                    _spin.Remove(k);
                    NetworkManager.Teleport(p, center);
                }
                else
                    _spin[k] = (center, angle, nr);
            }
        }

        private static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        private static void SendHostMessage(string coloredMessage) => SendHostMessage(coloredMessage, null);
        private static void SendHostMessage(string coloredMessage, string plainMessage)
        {
            Plugin.Log?.LogInfo($"[Director's Cut] {coloredMessage}");
            if (DirectorOptions.AnnounceInChat)
            {
                if (plainMessage == null)
                    ChatManager.Queue(coloredMessage);
                else
                    ChatManager.Queue(coloredMessage, plainMessage);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    static class Die_P
    { static void Postfix(PlayerControl __instance) => DirectorCore.OnPlayerDie(__instance); }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    static class RpcSendChat_P
    {
        static bool Prefix(PlayerControl __instance, string chatText)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId) return true;
            if (string.IsNullOrWhiteSpace(chatText)) return true;
            if (!chatText.TrimStart().StartsWith("/")) return true;
            DirectorCore.TryProcessCommand(__instance, chatText.Trim());
            return false;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    static class Chat_P
    {
        static bool Prefix(PlayerControl sourcePlayer, string chatText)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (ChatManager.IsSending) return true;
            if (chatText.StartsWith("/") && sourcePlayer?.PlayerId != PlayerControl.LocalPlayer.PlayerId)
            {
                DirectorCore.TryProcessCommand(sourcePlayer, chatText);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
    static class Visible_P { static bool Prefix(ChatController __instance) { __instance.gameObject.SetActive(true); return false; } }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    static class SendChat_P
    {
        static bool Prefix(ChatController __instance)
        {
            // If we are the host, allow sending messages always!
            if (AmongUsClient.Instance.AmHost)
            {
                return true;
            }

            // Check if we're in a meeting (allow sending if yes)
            if (MeetingHud.Instance != null)
            {
                return true;
            }

            // If in a game (not meeting) and not host, block sending
            if (ShipStatus.Instance != null)
            {
                return false;
            }

            return true; // Always allow in lobby
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    static class Start_P { static void Postfix() => DirectorCore.Reset(); }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.EndGame))]
    static class EndGame_P
    {
        static void Prefix()
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            DirectorCore.SnapshotEndState();
            Plugin.Log?.LogInfo("[DirectorCore] EndGame → snapshot taken");
        }
        static void Postfix()
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            DirectorCore.PendingAutoGG = true;
            Plugin.Log?.LogInfo("[DirectorCore] EndGame → GG pending");
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
    static class ExitGame_P
    {
        static void Postfix()
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            // Only snapshot if we haven't already (to keep existing data)
            if (DirectorCore.LastAlive.Count == 0 && DirectorCore.LastDead.Count == 0)
                DirectorCore.SnapshotEndState();
            DirectorCore.PendingAutoGG = true;
            Plugin.Log?.LogInfo("[DirectorCore] ExitGame → snapshot (if needed) + GG pending");
        }
    }
    
    [HarmonyPatch(typeof(ShipStatus), "OnDestroy")]
    static class ShipDestroy_P
    {
        static void Prefix()
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            // Only snapshot if we haven't already
            if (DirectorCore.LastAlive.Count == 0 && DirectorCore.LastDead.Count == 0)
                DirectorCore.SnapshotEndState();
            DirectorCore.PendingAutoGG = true;
            Plugin.Log?.LogInfo("[DirectorCore] ShipDestroy → snapshot (if needed) + GG pending");
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    static class HudUp_P
    {
        static void Postfix(HudManager __instance)
        {
            DirectorCore.Update();
            if (__instance?.Chat != null)
                __instance.Chat.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    static class Begin_P
    {
        static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            AmongUsClient.Instance.StartGame();
            return false;
        }
    }
}
