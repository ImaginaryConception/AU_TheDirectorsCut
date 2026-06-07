using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;
using InnerNet;
using AU_TheDirectorsCut.Hydra;

namespace AU_TheDirectorsCut
{
    public static class DirectorCore
    {
        public static byte? DirectorPlayerId { get; private set; }
        public static string? DirectorName { get; private set; }
        public static bool IsCutActive { get; private set; }
        public static bool PendingAutoGG { get; set; }
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
        private static readonly Dictionary<byte, float> _blindedPlayers = new();

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
            _blindedPlayers.Clear();
            _cd.Clear();
            NetworkManager.ResetGlobalVision();
            ChatManager.ClearWelcomeSent();
        }

        public static void OnPlayerDie(PlayerControl player)
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            if (player?.Data == null) return;
            if (DirectorPlayerId.HasValue) return;

            DirectorPlayerId = player.PlayerId;
            DirectorName = player.Data.PlayerName;

            Plugin.Log?.LogInfo(
                $"[Director] RÉALISATEUR attribué → \"{DirectorName}\" " +
                $"(PlayerId={player.PlayerId}, OwnerId={player.OwnerId})"
            );

            SendHostMessage(
                string.Format(ModMessages.FirstDirector, player.Data.PlayerName),
                string.Format(ModMessages.FirstDirectorPlain, player.Data.PlayerName)
            );
        }

        public static bool IsDirector(byte id) => DirectorPlayerId.HasValue && DirectorPlayerId.Value == id;

        private static string NumberToFrench(int number) => number.ToString();

        private static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

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

            bool isDevCommand = cmd == "/start" || cmd == "/stop" || cmd == "/setdirector";

            if (isDevCommand)
            {
                if (!DevModeManager.devMode)
                {
                    SendHostMessage($"Commande inconnue : {cmd} — /help");
                    return true;
                }

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
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                            SendHostMessage(ModMessages.GameStopped, ModMessages.GameStoppedPlain);
                            Plugin.Log?.LogInfo("[Director] /stop → RpcEndGame.");
                        }
                        catch (Exception e) { Plugin.Log?.LogError($"[/stop] {e.Message}"); }
                        return true;

                    case "/setdirector":
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
                                string.Format(ModMessages.DirectorSetPlain, dtarget.Data.PlayerName)
                            );
                        }
                        else
                        {
                            DirectorPlayerId = sender.PlayerId;
                            DirectorName = sender.Data.PlayerName;
                            SendHostMessage(
                                string.Format(ModMessages.DirectorSet, sender.Data.PlayerName),
                                string.Format(ModMessages.DirectorSetPlain, sender.Data.PlayerName)
                            );
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

                        const int maxLen = 100;
                        const int maxMessages = 8;

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

                            if ((plainCur + sep + plainPart).Length <= maxLen)
                            {
                                plainCur += sep + plainPart;
                                coloredCur += sep + coloredPart;
                                partsInCur++;
                            }
                            else
                            {
                                if (partsInCur > 0)
                                {
                                    chunksPlain.Add(plainCur);
                                    chunksColored.Add(coloredCur);
                                }
                                if (chunksPlain.Count >= maxMessages) { truncated = true; break; }
                                plainCur = plainPart;
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
                        NetworkManager.BlindPlayer(bt);
                        _blindedPlayers[bt.PlayerId] = Time.time + 8f;
                    }
                    else
                        SendHostMessage(ModMessages.UsageBlind, ModMessages.UsageBlindPlain);
                    return true;

                case "/darkness":
                    if (!TryCooldown("/darkness")) return true;
                    SendHostMessage("Darkness activée ! Vision globale réduite pendant 10 secondes !");
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
            SendCutSignal();
        }

        private static void AdvanceCutStep()
        {
            switch (cutStep)
            {
                case 1:
                    StopCutSignal();
                    foreach (var p in NetworkManager.Alive())
                        cutStartPositions[p.PlayerId] = p.GetTruePosition();
                    SendHostMessage(ModMessages.CutStart, ModMessages.CutStartPlain);
                    cutStep = 2; cutStepTimer = 5f;
                    break;
                case 2:
                    SendCutSignal();
                    cutStep = 3; cutStepTimer = 2f;
                    break;
                case 3:
                    StopCutSignal();
                    SendHostMessage(ModMessages.Sun, ModMessages.SunPlain);
                    IsCutActive = false; cutStep = 0;
                    break;
            }
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float dt = Time.deltaTime;

            if (ShipStatus.Instance != null)
            {
                _snapshotTimer -= dt;
                if (_snapshotTimer <= 0f)
                {
                    SnapshotEndState(false);
                    _snapshotTimer = 1f;
                }
            }

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
                    SendHostMessage("Vision globale restaurée");
                }
            }

            foreach (var kvp in _blindedPlayers.ToList())
            {
                byte playerId = kvp.Key;
                float endTime = kvp.Value;
                if (Time.time >= endTime)
                {
                    var player = FindById(playerId);
                    if (player != null)
                    {
                        NetworkManager.ResetPlayerVision(player);
                        SendHostMessage($"Vision de {player.Data.PlayerName} restaurée");
                    }
                    _blindedPlayers.Remove(playerId);
                }
            }

            float _effectTpTimer = 0f;
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
                float newAngle = angle + 3.5f * dt;
                if (tpTick) NetworkManager.Teleport(p, center + new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * 1.0f);
                float nr = rem - dt;
                if (nr <= 0f)
                {
                    _spin.Remove(k);
                    NetworkManager.Teleport(p, center);
                }
                else
                    _spin[k] = (center, newAngle, nr);
            }
        }

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

        private static SystemTypes CriticalSabotage()
        {
            var s = ShipStatus.Instance?.Systems;
            if (s == null) return SystemTypes.Reactor;
            if (s.ContainsKey(SystemTypes.Reactor)) return SystemTypes.Reactor;
            if (s.ContainsKey(SystemTypes.Laboratory)) return SystemTypes.Laboratory;
            if (s.ContainsKey(SystemTypes.HeliSabotage)) return SystemTypes.HeliSabotage;
            if (s.ContainsKey(SystemTypes.MushroomMixupSabotage)) return SystemTypes.MushroomMixupSabotage;
            return SystemTypes.Reactor;
        }

        private static void SendCutSignal()
        {
            if (ShipStatus.Instance == null || !AmongUsClient.Instance.AmHost) return;
            try
            {
                var sys = CriticalSabotage();
                ShipStatus.Instance.UpdateSystem(sys, PlayerControl.LocalPlayer, 128);
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (pc == null || pc.AmOwner || pc.OwnerId < 0) continue;
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, pc.OwnerId
                    );
                    writer.Write((byte)sys);
                    writer.WritePacked(PlayerControl.LocalPlayer.NetId);
                    writer.Write((byte)128);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            catch (Exception e) { Plugin.Log?.LogError($"[SendCutSignal] {e.Message}"); }
        }

        private static void StopCutSignal()
        {
            if (ShipStatus.Instance == null || !AmongUsClient.Instance.AmHost) return;
            try
            {
                var sys = CriticalSabotage();
                ShipStatus.Instance.UpdateSystem(sys, PlayerControl.LocalPlayer, 16);
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (pc == null || pc.AmOwner || pc.OwnerId < 0) continue;
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, pc.OwnerId
                    );
                    writer.Write((byte)sys);
                    writer.WritePacked(PlayerControl.LocalPlayer.NetId);
                    writer.Write((byte)16);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            catch (Exception e) { Plugin.Log?.LogError($"[StopCutSignal] {e.Message}"); }
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
            if (AmongUsClient.Instance.AmHost)
            {
                return true;
            }
            if (MeetingHud.Instance != null)
            {
                return true;
            }
            if (ShipStatus.Instance != null)
            {
                return false;
            }
            return true;
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

    // ── HYDRA PROTECTIONS PATCHES ────────────────────────────────────────────────
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SetEndpoint))]
    static class HydraForceDTLS
    {
        static void Prefix(ref bool dtls)
        {
            dtls = true;
        }
    }

    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
    static class HydraBlockServerTeleports
    {
        static bool Prefix(CustomNetworkTransform __instance, byte callId)
        {
            if (callId != (byte)RpcCalls.SnapTo || __instance.myPlayer != PlayerControl.LocalPlayer) return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    static class HydraVotekicks
    {
        static bool Prefix(int srcClient, int clientId)
        {
            if (clientId != PlayerControl.LocalPlayer.OwnerId) return true;
            return !AmongUsClient.Instance.AmHost;
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(InnerNetClient.CoStartGame))]
    static class HydraBypassShapeshiftRatelimits
    {
        static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            PlayerControl player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p != PlayerControl.LocalPlayer);
            if (player == null) return;
            IGameOptions options = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            options.SetFloat(FloatOptionNames.ShapeshifterCooldown, 0.0f);
            Hydra.GameOptions.SendGameOptionsToClient(options, player.OwnerId);
        }
    }
}
