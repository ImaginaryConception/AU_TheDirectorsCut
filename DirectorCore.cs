using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;
using InnerNet;
using AU_TheDirectorsCut.Utils;

namespace AU_TheDirectorsCut
{
    public static class DirectorCore
    {
        public static byte? DirectorPlayerId { get; private set; }
        public static string? DirectorName { get; private set; }
        public static int? DirectorOwnerId { get; private set; }
        public static bool PendingAutoGG { get; set; }
        private static float pendingAutoGGDelay = 0f;
        private static float _snapshotTimer = 0f;

        private static bool _cutActive = false;
        private static float _cutTimer = 0f;
        private static int _cutPhase = 0;
        private static Dictionary<byte, Vector2> _cutInitialPositions = new();
        private static List<byte> _cutKilledPlayers = new();

        private static readonly Dictionary<string, float> _cd = new();
        private static readonly Dictionary<string, float> _cdMax = new()
        {
            ["/randomcolors"] = 20f,
            ["/cut"] = 30f,
            ["/darkness"] = 35f,
            ["/freeze"] = 30f,
            ["/action"] = 20f,
            ["/loc"] = 20f,
            ["/vote"] = 20f,
            ["/colorblinds"] = 40f,
            ["/shuffle"] = 20f,
            ["/swap"] = 15f,
            ["/teleportall"] = 20f,
            ["/voiceover"] = 8f,
            ["/spotlight"] = 30f,
            ["/marathon"] = 30f,
            ["/quarantine"] = 30f,
            ["/roulette"] = 45f,
            ["/bodyswap"] = 30f,
            ["/tp"] = 10f,
        };

        private static Dictionary<byte, (float timer, Vector2 position, float originalSpeed)> _frozenPlayers = new();
        private static System.Collections.Generic.List<PlayerControl> _pendingPunishments = new();

        private static bool _darknessActive = false;
        private static float _darknessTimer = 0f;
        private static float _originalCrewLightMod = 1f;
        private static float _originalImpostorLightMod = 1f;

        // /colorblinds : effet temporisé (gris + noms masqués), restauration auto
        private const float ColorBlindDuration = 25f;
        private static bool _colorBlindActive = false;
        private static float _colorBlindTimer = 0f;
        private static readonly Dictionary<byte, (int colorId, string name)> _colorBlindOriginal = new();

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

        public static void AddPendingPunishment(PlayerControl player)
        {
            if (player == null || player.Data.IsDead) return;
            Plugin.Log?.LogInfo($"[DirectorCore] Adding pending punishment for {player.Data.PlayerName}");
            _pendingPunishments.Add(player);
        }

        public static void Initialize()
        {
            Reset();
            ScriptManager.Initialize();
            Plugin.Log?.LogInfo("[DirectorCore] Initialisé.");
        }

        public static void Reset()
        {
            DirectorPlayerId = null;
            DirectorName = null;
            DirectorOwnerId = null;
            PendingAutoGG = false;
            pendingAutoGGDelay = 0f;
            _cd.Clear();
            _cutActive = false;
            _cutPhase = 0;
            _cutTimer = 0f;
            _cutInitialPositions.Clear();
            _cutKilledPlayers.Clear();
            _darknessActive = false;
            _darknessTimer = 0f;
            _originalCrewLightMod = 1f;
            _originalImpostorLightMod = 1f;
            _colorBlindActive = false;
            _colorBlindTimer = 0f;
            _colorBlindOriginal.Clear();
            _frozenPlayers.Clear();
            _pendingPunishments.Clear();
            ScriptManager.Reset();
            ChatManager.ClearWelcomeSent();
            Directives.Reset();
        }

        public static void OnPlayerDie(PlayerControl player)
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            if (player?.Data == null) return;
            if (DirectorPlayerId.HasValue) return;

            DirectorPlayerId = player.PlayerId;
            DirectorName = player.Data.PlayerName;
            DirectorOwnerId = player.OwnerId;

            Plugin.Log?.LogInfo(
                $"[Director] RÉALISATEUR attribué → \"{DirectorName}\" " +
                $"(PlayerId={player.PlayerId}, OwnerId={player.OwnerId})"
            );

            // Confidentialité : on prévient en privé le nouveau Réalisateur, pas tout le monde.
            SendDirectorMessage(
                string.Format(ModMessages.FirstDirector, player.Data.PlayerName),
                string.Format(ModMessages.FirstDirectorPlain, player.Data.PlayerName)
            );
        }

        public static bool IsDirector(byte id) => DirectorPlayerId.HasValue && DirectorPlayerId.Value == id;

        private const float AntiKickWindow = 1.5f;

        public static bool TryRevertAntiCheatRetaliation(PlayerControl player)
        {
            if (AmongUsClient.Instance?.AmHost != true) return false;
            if (player != PlayerControl.LocalPlayer) return false;

            float elapsed = Time.time - NetworkManager.LastKillRpcSentAt;
            if (elapsed > AntiKickWindow) return false;

            Plugin.Log?.LogWarning(
                $"[AntiKick] L'hôte est mort {elapsed:F2}s après l'envoi de \"{NetworkManager.LastKillRpcDescription}\" — " +
                "probable riposte de l'anti-cheat sur le client hôte. Annulation automatique de la mort de l'hôte."
            );

            player.Data.IsDead = false;
            return true;
        }

        
        
        
        public static void HandlePlayerLeft(InnerNet.ClientData data)
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            if (data == null) return;
            if (!DirectorPlayerId.HasValue) return;

            
            
            bool directorLeft = false;

            if (DirectorOwnerId.HasValue && data.Id == DirectorOwnerId.Value)
            {
                directorLeft = true;
            }
            else if (data.Character != null && data.Character.PlayerId == DirectorPlayerId.Value)
            {
                directorLeft = true;
            }
            else if (FindById(DirectorPlayerId.Value) == null)
            {
                
                directorLeft = true;
            }

            if (!directorLeft) return;

            string formerName = DirectorName ?? "?";
            DirectorPlayerId = null;
            DirectorName = null;
            DirectorOwnerId = null;

            Plugin.Log?.LogInfo($"[Director] Le Réalisateur \"{formerName}\" a quitté la partie — poste VACANT. La prochaine mort deviendra le nouveau Réalisateur.");

            
            // Annonce PUBLIQUE (visible par tous)
            ChatManager.Queue(
                $"<b><color=#ffd23f>RÉALISATEUR</color></b> : {formerName} a quitté — poste vacant, la prochaine mort devient le nouveau Réalisateur.",
                $"RÉALISATEUR : {formerName} a quitté — poste vacant, la prochaine mort devient le nouveau Réalisateur."
            );
        }

        private static string NumberToFrench(int number) => number.ToString();

        private static bool TryParseZoneLetter(string letter, out MapLocation location)
        {
            location = MapLocation.Skeld_Admin;
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
                return false;

            char c = char.ToUpperInvariant(letter[0]);
            switch (c)
            {
                case 'B': location = MapLocation.Skeld_Admin; break;
                case 'C': location = MapLocation.Skeld_Electrical; break;
                case 'D': location = MapLocation.Skeld_Storage; break;
                case 'E': location = MapLocation.Skeld_Security; break;
                case 'F': location = MapLocation.Skeld_Reactor; break;
                case 'G': location = MapLocation.Skeld_UpperEngine; break;
                case 'H': location = MapLocation.Skeld_LowerEngine; break;
                case 'I': location = MapLocation.Skeld_Medbay; break;
                case 'J': location = MapLocation.Skeld_Communications; break;
                case 'K': location = MapLocation.Skeld_Shields; break;
                case 'L': location = MapLocation.Skeld_O2; break;
                case 'M': location = MapLocation.Skeld_Navigation; break;
                case 'N': location = MapLocation.Skeld_Weapons; break;
                default: return false;
            }
            return true;
        }

        private static bool TryParseScriptLetter(string letter, out ScriptOrder order)
        {
            order = ScriptOrder.NoReport;
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
                return false;

            char c = char.ToUpperInvariant(letter[0]);
            switch (c)
            {
                case 'A': order = ScriptOrder.NoReport; break;
                case 'B': order = ScriptOrder.SkipVote; break;
                case 'C': order = ScriptOrder.DontUseVents; break;
                case 'D': order = ScriptOrder.VoteFirst; break;
                default: return false;
            }
            return true;
        }

        internal static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        private static bool IsValidTarget(PlayerControl sender, PlayerControl target, out string errorMessage)
        {
            errorMessage = "";
            if (target == null || target?.Data == null)
            {
                errorMessage = ModMessages.PlayerNotFoundPlain;
                return false;
            }
            // L'hôte EST une cible valide (s'il est vivant) : l'ordre doit pouvoir lui
            // être envoyé et agir comme pour n'importe quel joueur.
            if (target.PlayerId == sender.PlayerId)
            {
                errorMessage = "Tu ne peux pas te choisir toi-même";
                return false;
            }
            if (target.Data.IsDead)
            {
                errorMessage = $"{target.Data.PlayerName} est éliminé(e)";
                return false;
            }
            if (target.Data.Disconnected)
            {
                errorMessage = $"{target.Data.PlayerName} est déconnecté(e)";
                return false;
            }
            return true;
        }

        private static void SendPrivateMessage(PlayerControl target, string message)
        {
            if (target == null || target.OwnerId < 0) return;
            ChatManager.SendSystemMessage(target, message, message);
        }

        private static bool TryCheckCooldown(string cmd, PlayerControl sender)
        {
            if (IsOnCooldown(cmd))
            {
                int remaining = Mathf.CeilToInt(CooldownRemaining(cmd));
                string remainingStr = NumberToFrench(remaining);
                string colored = string.Format(ModMessages.CooldownMsg, cmd, remainingStr);
                string plain = colored;
                ChatManager.QueueSystemMessage(sender, colored, plain);
                return false;
            }
            return true;
        }
 
        private static void SetCooldown(string cmd)
        {
            if (_cdMax.TryGetValue(cmd, out float max)) _cd[cmd] = max;
        }

        private static string PlayerIdToLetter(byte playerId)
        {
            return ((char)('A' + playerId)).ToString();
        }

        private static bool LetterToPlayerId(string letter, out byte playerId)
        {
            playerId = 0;
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
                return false;

            char c = char.ToUpperInvariant(letter[0]);
            if (c < 'A' || c > 'Z')
                return false;

            playerId = (byte)(c - 'A');
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

            bool isAdminCommand = cmd == "/start" || cmd == "/stop" || cmd == "/setdirector"
                || cmd == "/rename" || cmd == "/kill" || cmd == "/endmeeting"
                || cmd == "/kick" || cmd == "/status";

            if (isAdminCommand)
            {
                // Commandes ADMIN : réservées à l'hôte. Interdit pour tout autre joueur.
                if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                {
                    ChatManager.QueueSystemMessage(sender, ModMessages.HostOnly, ModMessages.HostOnlyPlain);
                    return true;
                }

                switch (cmd)
                {
                    case "/start":
                        if (!inLobby) { ChatManager.QueueSystemMessage(sender, "Pas en lobby !", "Pas en lobby !"); return true; }
                        try
                        {
                            // BeginGame() = le vrai bouton "Start" de l'hôte (gère le lancement réseau).
                            if (GameStartManager.Instance != null)
                                GameStartManager.Instance.BeginGame();
                            else
                                AmongUsClient.Instance.StartGame();
                        }
                        catch (Exception e) { Plugin.Log?.LogError($"[/start] {e.Message}"); }
                        return true;

                    case "/stop":
                        if (inLobby)
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.NoGameRunning, ModMessages.NoGameRunningPlain);
                            return true;
                        }
                        try
                        {
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                            ChatManager.QueueSystemMessage(sender, ModMessages.GameStopped, ModMessages.GameStoppedPlain);
                            Plugin.Log?.LogInfo("[Director] /stop → RpcEndGame.");
                        }
                        catch (Exception e) { Plugin.Log?.LogError($"[/stop] {e.Message}"); }
                        return true;

                    case "/rename":
                        if (parts.Length < 3)
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.UsageRename, ModMessages.UsageRenamePlain);
                            return true;
                        }
                        if (!LetterToPlayerId(parts[1], out byte rnId))
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        PlayerControl? rnTarget = FindById(rnId);
                        if (rnTarget?.Data == null)
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        string rnOld = rnTarget.Data.PlayerName;
                        string rnNew = string.Join(" ", parts, 2, parts.Length - 2);
                        NetworkManager.SetPlayerName(rnTarget, rnNew);
                        ChatManager.QueueSystemMessage(sender,
                            string.Format(ModMessages.RenameDone, rnOld, rnNew),
                            string.Format(ModMessages.RenameDonePlain, rnOld, rnNew)
                        );
                        return true;

                    case "/kill":
                        if (inLobby)
                        {
                            ChatManager.QueueSystemMessage(sender, "Pas en jeu !", "Pas en jeu !");
                            return true;
                        }
                        if (parts.Length < 2)
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.UsageKill, ModMessages.UsageKillPlain);
                            return true;
                        }
                        if (!LetterToPlayerId(parts[1], out byte klId))
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        PlayerControl? klTarget = FindById(klId);
                        if (klTarget?.Data == null)
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        if (klTarget.Data.IsDead)
                        {
                            ChatManager.QueueSystemMessage(sender, $"{klTarget.Data.PlayerName} est déjà éliminé(e)", $"{klTarget.Data.PlayerName} est déjà éliminé(e)");
                            return true;
                        }
                        NetworkManager.MurderPlayer(klTarget);
                        ChatManager.QueueSystemMessage(sender,
                            string.Format(ModMessages.KillSuccess, klTarget.Data.PlayerName),
                            string.Format(ModMessages.KillSuccessPlain, klTarget.Data.PlayerName)
                        );
                        return true;

                    case "/endmeeting":
                        if (MeetingHud.Instance == null)
                        {
                            ChatManager.QueueSystemMessage(sender, "Aucune réunion en cours !", "Aucune réunion en cours !");
                            return true;
                        }
                        try
                        {
                            // Force la fin : le timer dépasse le total discussion+vote → clôture
                            MeetingHud.Instance.discussionTimer = 9999f;
                            ChatManager.QueueSystemMessage(sender, ModMessages.MeetingEnded, ModMessages.MeetingEndedPlain);
                            Plugin.Log?.LogInfo("[Admin] /endmeeting → réunion forcée à se terminer.");
                        }
                        catch (Exception e) { Plugin.Log?.LogError($"[/endmeeting] {e.Message}"); }
                        return true;

                    case "/kick":
                        if (parts.Length < 2 || !LetterToPlayerId(parts[1], out byte kkId))
                        {
                            ChatManager.QueueSystemMessage(sender, "Usage : /kick ID", "Usage : /kick ID");
                            return true;
                        }
                        PlayerControl? kkTarget = FindById(kkId);
                        if (kkTarget?.Data == null)
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                            return true;
                        }
                        if (kkTarget.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            ChatManager.QueueSystemMessage(sender, "Tu ne peux pas te kick toi-même !", "Tu ne peux pas te kick toi-même !");
                            return true;
                        }
                        try
                        {
                            AmongUsClient.Instance.KickPlayer(kkTarget.OwnerId, false);
                            ChatManager.QueueSystemMessage(sender, $"<b><color=#ff4d4d>{kkTarget.Data.PlayerName}</color></b> a été exclu.", $"{kkTarget.Data.PlayerName} a été exclu.");
                        }
                        catch (Exception e) { Plugin.Log?.LogError($"[/kick] {e.Message}"); }
                        return true;

                    case "/status":
                        {
                            var fx = new List<string>();
                            if (_cutActive) fx.Add("Cut en cours");
                            if (_darknessActive) fx.Add("Darkness");
                            if (_colorBlindActive) fx.Add("Colorblind");
                            if (_frozenPlayers.Count > 0) fx.Add($"{_frozenPlayers.Count} gelé(s)");
                            string dir = Directives.Status();
                            if (!string.IsNullOrEmpty(dir)) fx.Add(dir);
                            string body = fx.Count == 0 ? "Aucun effet actif." : string.Join(", ", fx);
                            string colored = $"<b><color=#ffd23f>État</color></b>\nRéalisateur : {DirectorName ?? "aucun"}\n{body}";
                            string plain = $"Etat - Realisateur : {DirectorName ?? "aucun"} - {body}";
                            ChatManager.QueueSystemMessage(sender, colored, plain);
                        }
                        return true;

                    case "/setdirector":
                        if (parts.Length >= 2 && LetterToPlayerId(parts[1], out byte did))
                        {
                            var dtarget = FindById(did);
                            if (dtarget?.Data == null)
                            {
                                ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                                return true;
                            }
                            DirectorPlayerId = dtarget.PlayerId;
                            DirectorName = dtarget.Data.PlayerName;
                            DirectorOwnerId = dtarget.OwnerId;
                            ChatManager.QueueSystemMessage(sender,
                                string.Format(ModMessages.DirectorSet, dtarget.Data.PlayerName),
                                string.Format(ModMessages.DirectorSetPlain, dtarget.Data.PlayerName)
                            );
                        }
                        else
                        {
                            DirectorPlayerId = sender.PlayerId;
                            DirectorName = sender.Data.PlayerName;
                            DirectorOwnerId = sender.OwnerId;
                            ChatManager.QueueSystemMessage(sender,
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
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.Welcome, ModMessages.WelcomePlain);
                    return true;

                case "/help":
                    {
                        // UN seul message : tout est affiché, stylisé. La ligne Admin n'est
                        // ajoutée que pour l'hôte.
                        string helpColored = ModMessages.HelpAll;
                        string helpPlain = ModMessages.HelpAllPlain;
                        if (sender.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            helpColored += "\n" + ModMessages.HelpAdminLine;
                            helpPlain += "\n" + ModMessages.HelpAdminLinePlain;
                        }
                        ChatManager.QueueSystemMessage(sender, helpColored, helpPlain);
                    }
                    return true;

                case "/gg":
                    ChatManager.SendPrivateGGToAll();
                    return true;

                case "/join":
                case "/discord":
                    {
                        string link = ModConfig.DiscordLink?.Value;
                        if (string.IsNullOrWhiteSpace(link))
                            ChatManager.QueueSystemMessage(sender, ModMessages.Discord, ModMessages.DiscordPlain);
                        else
                            ChatManager.QueueSystemMessage(sender, $"<b><color=#5865F2>Discord</color></b> : <color=#5865F2><u>{link}</u></color>", $"Discord : {link}");
                    }
                    return true;

                case "/cooldowns":
                case "/cd":
                    {
                        var sb = new System.Text.StringBuilder("<b><color=#ffd23f>Cooldowns</color></b>");
                        foreach (var kvp in _cdMax)
                        {
                            float rem = CooldownRemaining(kvp.Key);
                            sb.Append($"\n{kvp.Key} : {(rem > 0f ? Mathf.CeilToInt(rem) + "s" : "<color=#00e676>prêt</color>")}");
                        }
                        string cdTxt = sb.ToString();
                        ChatManager.QueueSystemMessage(sender, cdTxt, cdTxt);
                    }
                    return true;

                case "/players":
                    {
                        var players = PlayerControl.AllPlayerControls.ToArray()
                            .Where(p => p?.Data != null)
                            .OrderBy(p => p.PlayerId)
                            .ToList();

                        // Un seul message, une ligne par joueur (gras + couleur)
                        var sbColored = new System.Text.StringBuilder("<b><color=#ffd23f>Joueurs</color></b>");
                        var sbPlain = new System.Text.StringBuilder("Joueurs");
                        foreach (var p in players)
                        {
                            string letter = PlayerIdToLetter(p.PlayerId);
                            string deadColored = p.Data.IsDead ? " <color=#ff6b6b>(éliminé)</color>" : "";
                            string deadPlain = p.Data.IsDead ? " (éliminé)" : "";
                            sbColored.Append($"\n<b><color=#3B9DFF>{letter}</color></b> {p.Data.PlayerName}{deadColored}");
                            sbPlain.Append($"\n{letter} {p.Data.PlayerName}{deadPlain}");
                        }

                        ChatManager.QueueSystemMessage(sender, sbColored.ToString(), sbPlain.ToString());
                        return true;
                    }

                case "/hrandomcolors":
                case "/hrandomcolor":
                case "/handomcolors":
                case "/hrandom":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpRandomColors, ModMessages.HelpRandomColorsPlain);
                    return true;
                case "/hcut":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpCut, ModMessages.HelpCutPlain);
                    return true;
                case "/hdarkness":
                case "/hdark":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpDarkness, ModMessages.HelpDarknessPlain);
                    return true;
                case "/hfreeze":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpFreeze, ModMessages.HelpFreezePlain);
                    return true;
                case "/haction":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionFull, ModMessages.HelpActionFullPlain);
                    return true;
                case "/helpaction":
                    if (parts.Length == 2)
                    {
                        if (TryParseScriptLetter(parts[1], out ScriptOrder order))
                        {
                            switch (order)
                            {
                                case ScriptOrder.NoReport:
                                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionA, ModMessages.HelpActionAPlain);
                                    break;
                                case ScriptOrder.SkipVote:
                                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionB, ModMessages.HelpActionBPlain);
                                    break;
                                case ScriptOrder.DontUseVents:
                                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionC, ModMessages.HelpActionCPlain);
                                    break;
                                case ScriptOrder.VoteFirst:
                                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionD, ModMessages.HelpActionDPlain);
                                    break;
                            }
                        }
                        else
                        {
                            ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionFull, ModMessages.HelpActionFullPlain);
                        }
                    }
                    else
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionFull, ModMessages.HelpActionFullPlain);
                    }
                    return true;
                case "/hloc":
                case "/hollow":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpLocFull, ModMessages.HelpLocFullPlain);
                    return true;
                case "/hvote":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpVote, ModMessages.HelpVotePlain);
                    return true;

                // ---- Aides des commandes générales ----
                case "/hhelp":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HHelp, ModMessages.HHelpPlain);
                    return true;
                case "/hwelcome":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HWelcome, ModMessages.HWelcomePlain);
                    return true;
                case "/hgg":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HGg, ModMessages.HGgPlain);
                    return true;
                case "/hplayers":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HPlayers, ModMessages.HPlayersPlain);
                    return true;
                case "/hdiscord":
                case "/hjoin":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HDiscord, ModMessages.HDiscordPlain);
                    return true;
                case "/hcooldowns":
                case "/hcd":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HCooldowns, ModMessages.HCooldownsPlain);
                    return true;

                // ---- Aides des commandes Réalisateur (en jeu) ----
                case "/hcolorblind":
                case "/hcolorblinds":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HColorblind, ModMessages.HColorblindPlain);
                    return true;
                case "/hshuffle":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HShuffle, ModMessages.HShufflePlain);
                    return true;
                case "/hswap":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HSwap, ModMessages.HSwapPlain);
                    return true;
                case "/hteleportall":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HTeleportall, ModMessages.HTeleportallPlain);
                    return true;
                case "/htp":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HTp, ModMessages.HTpPlain);
                    return true;
                case "/hvoiceover":
                case "/hvoixoff":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HVoiceover, ModMessages.HVoiceoverPlain);
                    return true;
                case "/hspotlight":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HSpotlight, ModMessages.HSpotlightPlain);
                    return true;
                case "/hmarathon":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HMarathon, ModMessages.HMarathonPlain);
                    return true;
                case "/hquarantine":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HQuarantine, ModMessages.HQuarantinePlain);
                    return true;
                case "/hroulette":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HRoulette, ModMessages.HRoulettePlain);
                    return true;
                case "/hbodyswap":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HBodyswap, ModMessages.HBodyswapPlain);
                    return true;

                // ---- Aides des commandes Réalisateur (en réunion) ----
                case "/hstalker":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HStalker, ModMessages.HStalkerPlain);
                    return true;
                case "/hultimatum":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HUltimatum, ModMessages.HUltimatumPlain);
                    return true;

                // ---- Aides des commandes Admin ----
                case "/hstart":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HStart, ModMessages.HStartPlain);
                    return true;
                case "/hstop":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HStop, ModMessages.HStopPlain);
                    return true;
                case "/hsetdirector":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HSetdirector, ModMessages.HSetdirectorPlain);
                    return true;
                case "/hrename":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HRename, ModMessages.HRenamePlain);
                    return true;
                case "/hkill":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HKill, ModMessages.HKillPlain);
                    return true;
                case "/hkick":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HKick, ModMessages.HKickPlain);
                    return true;
                case "/hendmeeting":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HEndmeeting, ModMessages.HEndmeetingPlain);
                    return true;
                case "/hstatus":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HStatus, ModMessages.HStatusPlain);
                    return true;
            }

            if (inLobby)
            {
                Plugin.Log?.LogInfo($"[DirectorCore] {cmd} ignoré en lobby.");
                return true;
            }

            if (!IsDirector(sender.PlayerId))
            {
                ChatManager.QueueSystemMessage(sender,
                    string.Format(ModMessages.NotDirector, sender.Data.PlayerName),
                    string.Format(ModMessages.NotDirectorPlain, sender.Data.PlayerName)
                );
                return true;
            }

            switch (cmd)
            {
                case "/randomcolors":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/randomcolors", sender)) return true;
                    SendDirectorMessage(ModMessages.RandomColorsStart, ModMessages.RandomColorsStartPlain);
                    NetworkManager.RandomizeColors();
                    SetCooldown("/randomcolors");
                    return true;

                case "/cut":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/cut", sender)) return true;
                    StartCutSequence();
                    SetCooldown("/cut");
                    return true;

                case "/darkness":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/darkness", sender)) return true;
                    StartDarkness();
                    SetCooldown("/darkness");
                    return true;

                case "/freeze":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/freeze", sender)) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessage(sender, "Usage : /freeze LETTRE (ex: /freeze A)", "Usage : /freeze LETTRE (ex: /freeze A)");
                        return true;
                    }
                    if (!LetterToPlayerId(parts[1], out byte freezeTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? freezeTarget = FindById(freezeTargetId);
                    if (freezeTarget?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (_frozenPlayers.ContainsKey(freezeTarget.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender, $"{freezeTarget.Data.PlayerName} est déjà bloqué !", $"{freezeTarget.Data.PlayerName} est déjà bloqué !");
                        return true;
                    }
                    StartFreeze(freezeTarget);
                    SetCooldown("/freeze");
                    return true;

                case "/action":
                    if (!IsDirector(sender.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HostOnly, ModMessages.HostOnlyPlain);
                        return true;
                    }
                    if (MeetingHud.Instance == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/action", sender)) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionFull, ModMessages.HelpActionFullPlain);
                        return true;
                    }
                    if (!LetterToPlayerId(parts[1], out byte actionTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? actionTarget = FindById(actionTargetId);
                    if (!IsValidTarget(sender, actionTarget, out string actionError))
                    {
                        ChatManager.QueueSystemMessage(sender, actionError, actionError);
                        return true;
                    }
                    
                    if (parts.Length < 3)
                    {
                        
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionFull, ModMessages.HelpActionFullPlain);
                        return true;
                    }
                    
                    if (!TryParseScriptLetter(parts[2], out ScriptOrder order))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpActionFull, ModMessages.HelpActionFullPlain);
                        return true;
                    }
                    
                    
                    if (order == ScriptOrder.StayOut || order == ScriptOrder.VoteForPlayer)
                    {
                        ChatManager.QueueSystemMessage(sender, "Utilisez /loc ou /vote pour ces ordres !", "Utilisez /loc ou /vote pour ces ordres !");
                        return true;
                    }
                    
                    if (actionTarget?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (ScriptManager.HasScript(actionTarget.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender,
                            string.Format(ModMessages.ActionAlreadyActive, actionTarget.Data.PlayerName),
                            string.Format(ModMessages.ActionAlreadyActivePlain, actionTarget.Data.PlayerName)
                        );
                        return true;
                    }
                    
                    ScriptManager.AssignScript(actionTarget.PlayerId, order);
                    
                    
                    var (plainMsg, coloredMsg) = ScriptManager.GetOrderPrivateMessages(order, actionTarget.Data.PlayerName);
                    ChatManager.QueueSystemMessage(actionTarget, coloredMsg, plainMsg);
                    // Confirmation privée à l'hôte ET au réalisateur
                    SendDirectorMessage(
                        string.Format(ModMessages.ActionAssigned, actionTarget.Data.PlayerName),
                        string.Format(ModMessages.ActionAssignedPlain, actionTarget.Data.PlayerName)
                    );
                    SetCooldown("/action");
                    return true;
                    
                case "/loc":
                    if (!IsDirector(sender.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HostOnly, ModMessages.HostOnlyPlain);
                        return true;
                    }
                    if (MeetingHud.Instance == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/loc", sender)) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpLocFull, ModMessages.HelpLocFullPlain);
                        return true;
                    }
                    if (!LetterToPlayerId(parts[1], out byte locTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? locTarget = FindById(locTargetId);
                    if (!IsValidTarget(sender, locTarget, out string locError))
                    {
                        ChatManager.QueueSystemMessage(sender, locError, locError);
                        return true;
                    }
                    
                    if (parts.Length < 3)
                    {
                        
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpLocFull, ModMessages.HelpLocFullPlain);
                        return true;
                    }
                    
                    if (!TryParseZoneLetter(parts[2], out MapLocation location))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HelpLocFull, ModMessages.HelpLocFullPlain);
                        return true;
                    }
                    
                    if (locTarget?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (ScriptManager.HasScript(locTarget.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender,
                            string.Format(ModMessages.ActionAlreadyActive, locTarget.Data.PlayerName),
                            string.Format(ModMessages.ActionAlreadyActivePlain, locTarget.Data.PlayerName)
                        );
                        return true;
                    }
                    
                    ScriptManager.AssignStayOutScript(locTarget.PlayerId, location);
                    
                    
                    var (locPlain, locColored) = ScriptManager.GetStayOutPrivateMessages(location, locTarget.Data.PlayerName);
                    ChatManager.QueueSystemMessage(locTarget, locColored, locPlain);
                    // Confirmation privée à l'hôte ET au réalisateur
                    SendDirectorMessage(
                        string.Format(ModMessages.LocAssigned, locTarget.Data.PlayerName),
                        string.Format(ModMessages.LocAssignedPlain, locTarget.Data.PlayerName)
                    );
                    SetCooldown("/loc");
                    return true;
                    
                case "/vote":
                    if (!IsDirector(sender.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.HostOnly, ModMessages.HostOnlyPlain);
                        return true;
                    }
                    if (MeetingHud.Instance == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/vote", sender)) return true;
                    if (parts.Length < 3)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.UsageVote, ModMessages.UsageVotePlain);
                        return true;
                    }
                    if (!LetterToPlayerId(parts[1], out byte voteTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (!LetterToPlayerId(parts[2], out byte voteForId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    PlayerControl? voteTarget = FindById(voteTargetId);
                    PlayerControl? voteForTarget = FindById(voteForId);
                    
                    if (voteTarget?.Data == null || voteForTarget?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (!IsValidTarget(sender, voteTarget, out string voteTargetError))
                    {
                        ChatManager.QueueSystemMessage(sender, voteTargetError, voteTargetError);
                        return true;
                    }
                    if (voteForTarget.Data.IsDead)
                    {
                        ChatManager.QueueSystemMessage(sender, $"{voteForTarget.Data.PlayerName} est éliminé(e)", $"{voteForTarget.Data.PlayerName} est éliminé(e)");
                        return true;
                    }
                    if (voteForTarget.Data.Disconnected)
                    {
                        ChatManager.QueueSystemMessage(sender, $"{voteForTarget.Data.PlayerName} est déconnecté(e)", $"{voteForTarget.Data.PlayerName} est déconnecté(e)");
                        return true;
                    }
                    
                    if (ScriptManager.HasScript(voteTarget.PlayerId))
                    {
                        ChatManager.QueueSystemMessage(sender,
                            string.Format(ModMessages.ActionAlreadyActive, voteTarget.Data.PlayerName),
                            string.Format(ModMessages.ActionAlreadyActivePlain, voteTarget.Data.PlayerName)
                        );
                        return true;
                    }
                    
                    ScriptManager.AssignVoteForPlayerScript(voteTarget.PlayerId, voteForId);
                    
                    
                    var (votePlain, voteColored) = ScriptManager.GetVoteForPlayerPrivateMessages(voteForTarget.Data.PlayerName, voteTarget.Data.PlayerName);
                    ChatManager.QueueSystemMessage(voteTarget, voteColored, votePlain);
                    // Confirmation privée à l'hôte ET au réalisateur
                    SendDirectorMessage(
                        string.Format(ModMessages.VoteAssigned, voteTarget.Data.PlayerName),
                        string.Format(ModMessages.VoteAssignedPlain, voteTarget.Data.PlayerName)
                    );
                    SetCooldown("/vote");
                    return true;

                case "/colorblinds":
                case "/colorblind":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/colorblinds", sender)) return true;
                    if (_colorBlindActive)
                    {
                        ChatManager.QueueSystemMessage(sender, "Colorblind est déjà actif !", "Colorblind est déjà actif !");
                        return true;
                    }
                    StartColorBlind();
                    SetCooldown("/colorblinds");
                    return true;

                case "/shuffle":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/shuffle", sender)) return true;
                    SendDirectorMessage(ModMessages.ShuffleStart, ModMessages.ShuffleStartPlain);
                    NetworkManager.ShuffleAllPlayers();
                    SetCooldown("/shuffle");
                    return true;

                case "/swap":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/swap", sender)) return true;
                    if (parts.Length < 3)
                    {
                        ChatManager.QueueSystemMessage(sender, "Usage : /swap IDA IDB (ex: /swap A B)", "Usage : /swap IDA IDB (ex: /swap A B)");
                        return true;
                    }
                    if (!LetterToPlayerId(parts[1], out byte swapAId) || !LetterToPlayerId(parts[2], out byte swapBId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? swapA = FindById(swapAId);
                    PlayerControl? swapB = FindById(swapBId);
                    if (swapA?.Data == null || swapB?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (swapA.PlayerId == swapB.PlayerId)
                    {
                        ChatManager.QueueSystemMessage(sender, "Choisis deux joueurs différents !", "Choisis deux joueurs différents !");
                        return true;
                    }
                    NetworkManager.SwapPlayers(swapA, swapB);
                    SendDirectorMessage(
                        string.Format(ModMessages.SwapDone, swapA.Data.PlayerName, swapB.Data.PlayerName),
                        string.Format(ModMessages.SwapDonePlain, swapA.Data.PlayerName, swapB.Data.PlayerName)
                    );
                    SetCooldown("/swap");
                    return true;

                case "/teleportall":
                    if (MeetingHud.Instance != null)
                    {
                        ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/teleportall", sender)) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessage(sender, "Usage : /teleportall ID (ex: /teleportall A)", "Usage : /teleportall ID (ex: /teleportall A)");
                        return true;
                    }
                    if (!LetterToPlayerId(parts[1], out byte tpAllId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? tpAllTarget = FindById(tpAllId);
                    if (tpAllTarget?.Data == null || tpAllTarget.Data.Disconnected)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    NetworkManager.TeleportAllTo(tpAllTarget);
                    SendDirectorMessage(
                        string.Format(ModMessages.TeleportAllDone, tpAllTarget.Data.PlayerName),
                        string.Format(ModMessages.TeleportAllDonePlain, tpAllTarget.Data.PlayerName)
                    );
                    SetCooldown("/teleportall");
                    return true;

                case "/tp":
                    if (MeetingHud.Instance != null) { ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !"); return true; }
                    if (!TryCheckCooldown("/tp", sender)) return true;
                    if (parts.Length < 3 || !LetterToPlayerId(parts[1], out byte tpA) || !LetterToPlayerId(parts[2], out byte tpB))
                    {
                        ChatManager.QueueSystemMessage(sender, "Usage : /tp IDA IDB (téléporte A vers B)", "Usage : /tp IDA IDB (téléporte A vers B)");
                        return true;
                    }
                    PlayerControl? tpTa = FindById(tpA);
                    PlayerControl? tpTb = FindById(tpB);
                    if (tpTa?.Data == null || tpTb?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (tpTa.PlayerId == tpTb.PlayerId)
                    {
                        ChatManager.QueueSystemMessage(sender, "Choisis deux joueurs différents !", "Choisis deux joueurs différents !");
                        return true;
                    }
                    NetworkManager.Teleport(tpTa, tpTb.GetTruePosition());
                    SendDirectorMessage($"<b>{tpTa.Data.PlayerName}</b> téléporté vers <b>{tpTb.Data.PlayerName}</b>.", $"{tpTa.Data.PlayerName} téléporté vers {tpTb.Data.PlayerName}.");
                    SetCooldown("/tp");
                    return true;

                // ===================== DIRECTIVES (en jeu) =====================
                case "/voiceover":
                case "/voixoff":
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessage(sender, "Usage : /voiceover <texte>", "Usage : /voiceover <texte>");
                        return true;
                    }
                    if (!TryCheckCooldown("/voiceover", sender)) return true;
                    Directives.VoiceOver(string.Join(" ", parts, 1, parts.Length - 1));
                    SetCooldown("/voiceover");
                    return true;

                case "/spotlight":
                    if (MeetingHud.Instance != null) { ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !"); return true; }
                    if (!TryCheckCooldown("/spotlight", sender)) return true;
                    if (parts.Length < 2 || !LetterToPlayerId(parts[1], out byte spId)) { ChatManager.QueueSystemMessage(sender, "Usage : /spotlight ID", "Usage : /spotlight ID"); return true; }
                    PlayerControl? spTarget = FindById(spId);
                    if (spTarget?.Data == null) { ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain); return true; }
                    Directives.Spotlight(spTarget);
                    SetCooldown("/spotlight");
                    return true;

                case "/marathon":
                    if (MeetingHud.Instance != null) { ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !"); return true; }
                    if (!TryCheckCooldown("/marathon", sender)) return true;
                    Directives.Marathon();
                    SetCooldown("/marathon");
                    return true;

                case "/quarantine":
                    if (MeetingHud.Instance != null) { ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !"); return true; }
                    if (!TryCheckCooldown("/quarantine", sender)) return true;
                    if (parts.Length < 2 || !LetterToPlayerId(parts[1], out byte qId)) { ChatManager.QueueSystemMessage(sender, "Usage : /quarantine ID", "Usage : /quarantine ID"); return true; }
                    PlayerControl? qTarget = FindById(qId);
                    if (qTarget?.Data == null) { ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain); return true; }
                    Directives.Quarantine(qTarget);
                    SetCooldown("/quarantine");
                    return true;

                case "/roulette":
                    if (MeetingHud.Instance != null) { ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !"); return true; }
                    if (!TryCheckCooldown("/roulette", sender)) return true;
                    Directives.Roulette();
                    SetCooldown("/roulette");
                    return true;

                case "/bodyswap":
                    if (MeetingHud.Instance != null) { ChatManager.QueueSystemMessage(sender, "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !"); return true; }
                    if (!TryCheckCooldown("/bodyswap", sender)) return true;
                    if (parts.Length < 3 || !LetterToPlayerId(parts[1], out byte bsA) || !LetterToPlayerId(parts[2], out byte bsB)) { ChatManager.QueueSystemMessage(sender, "Usage : /bodyswap IDA IDB", "Usage : /bodyswap IDA IDB"); return true; }
                    PlayerControl? bsTa = FindById(bsA); PlayerControl? bsTb = FindById(bsB);
                    if (bsTa?.Data == null || bsTb?.Data == null) { ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain); return true; }
                    if (bsTa.PlayerId == bsTb.PlayerId) { ChatManager.QueueSystemMessage(sender, "Choisis deux joueurs différents !", "Choisis deux joueurs différents !"); return true; }
                    Directives.BodySwap(bsTa, bsTb);
                    SetCooldown("/bodyswap");
                    return true;

                // ===================== DIRECTIVES (réunion) =====================
                case "/stalker":
                    if (MeetingHud.Instance == null) { ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain); return true; }
                    if (parts.Length < 3 || !LetterToPlayerId(parts[1], out byte skA) || !LetterToPlayerId(parts[2], out byte skB)) { ChatManager.QueueSystemMessage(sender, "Usage : /stalker IDA IDB (A doit suivre B)", "Usage : /stalker IDA IDB (A doit suivre B)"); return true; }
                    PlayerControl? skTa = FindById(skA); PlayerControl? skTb = FindById(skB);
                    if (skTa?.Data == null || skTb?.Data == null) { ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain); return true; }
                    if (skTa.PlayerId == skTb.PlayerId) { ChatManager.QueueSystemMessage(sender, "Choisis deux joueurs différents !", "Choisis deux joueurs différents !"); return true; }
                    Directives.RegisterStalker(skTa, skTb);
                    return true;

                case "/ultimatum":
                    if (MeetingHud.Instance == null) { ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain); return true; }
                    if (parts.Length < 2 || !LetterToPlayerId(parts[1], out byte ulId)) { ChatManager.QueueSystemMessage(sender, "Usage : /ultimatum ID [secondes] (un Imposteur)", "Usage : /ultimatum ID [secondes] (un Imposteur)"); return true; }
                    PlayerControl? ulTarget = FindById(ulId);
                    if (ulTarget?.Data == null) { ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain); return true; }
                    float ulSec = 0f;
                    if (parts.Length >= 3) float.TryParse(parts[2], out ulSec);
                    Directives.Ultimatum(ulTarget, ulSec);
                    return true;

                default:
                    ChatManager.QueueSystemMessage(sender, $"Commande inconnue : {cmd} — /help", $"Commande inconnue : {cmd} — /help");
                    return true;
            }
        }

        private static void StartCutSequence()
        {
            _cutActive = true;
            _cutPhase = 1;
            _cutTimer = 2f;
            _cutInitialPositions.Clear();
            _cutKilledPlayers.Clear();

            
            // Privé au Réalisateur (confidentialité) — les joueurs ne sont pas prévenus
            SendDirectorMessage(ModMessages.CutStart, ModMessages.CutStartPlain);

            
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                // L'hôte est désormais inclus : /cut peut le tuer aussi s'il bouge.
                if (pc?.Data != null && !pc.Data.IsDead && !pc.Data.Disconnected)
                {
                    _cutInitialPositions[pc.PlayerId] = (Vector2)pc.transform.position;
                }
            }

            
            TriggerReactorSabotage(true);
        }

        private static void TriggerReactorSabotage(bool active)
        {
            if (ShipStatus.Instance == null) return;

            
            SystemTypes reactorType = SystemTypes.Reactor;
            MapNames map = (MapNames)GameManager.Instance.LogicOptions.currentGameOptions.GetByte(ByteOptionNames.MapId);
            switch (map)
            {
                case MapNames.Polus:
                    reactorType = SystemTypes.Laboratory;
                    break;
                case MapNames.Airship:
                    reactorType = SystemTypes.HeliSabotage;
                    break;
            }

            if (active)
            {
                
                ShipStatus.Instance.RpcUpdateSystem(reactorType, 128);
            }
            else
            {
                
                ShipStatus.Instance.RpcUpdateSystem(reactorType, 16);
            }
        }

        private static void HydraKillPlayer(PlayerControl target)
        {
            NetworkManager.MurderPlayer(target);
        }

        private static void StartDarkness()
        {
            if (ShipStatus.Instance == null) return;

            
            _originalCrewLightMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.CrewLightMod);
            _originalImpostorLightMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);

            _darknessActive = true;
            _darknessTimer = 10f;

            
            SendDirectorMessage(ModMessages.DarknessStart, ModMessages.DarknessStartPlain);

            
            Plugin.Log?.LogInfo("[DirectorCore.StartDarkness] Starting darkness for all players:");
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null || pc.Data.Disconnected) continue;

                Plugin.Log?.LogInfo($"[DirectorCore.StartDarkness] Blinding player {pc.Data.PlayerName} (OwnerId: {pc.OwnerId}, PlayerId: {pc.PlayerId})");
                IGameOptions blindOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                blindOptions.SetFloat(FloatOptionNames.CrewLightMod, 0.0f);
                blindOptions.SetFloat(FloatOptionNames.ImpostorLightMod, 0.0f);
                Utils.GameOptions.SendGameOptionsToClient(blindOptions, pc.OwnerId);
            }
        }

        private static void EndDarkness()
        {
            if (ShipStatus.Instance == null) return;

            _darknessActive = false;
            _darknessTimer = 0f;

            
            SendDirectorMessage(ModMessages.DarknessEnd, ModMessages.DarknessEndPlain);

            
            Plugin.Log?.LogInfo("[DirectorCore.EndDarkness] Restoring vision for all players:");
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null || pc.Data.Disconnected) continue;

                Plugin.Log?.LogInfo($"[DirectorCore.EndDarkness] Restoring vision for {pc.Data.PlayerName} (OwnerId: {pc.OwnerId}, PlayerId: {pc.PlayerId})");
                IGameOptions normalOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                normalOptions.SetFloat(FloatOptionNames.CrewLightMod, _originalCrewLightMod);
                normalOptions.SetFloat(FloatOptionNames.ImpostorLightMod, _originalImpostorLightMod);
                Utils.GameOptions.SendGameOptionsToClient(normalOptions, pc.OwnerId);
            }
        }

        private static void StartColorBlind()
        {
            if (ShipStatus.Instance == null) return;

            // Mémorise couleurs + noms d'origine pour pouvoir restaurer
            _colorBlindOriginal.Clear();
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null || pc.Data.Disconnected) continue;
                _colorBlindOriginal[pc.PlayerId] = (pc.Data.DefaultOutfit.ColorId, pc.Data.PlayerName);
            }

            _colorBlindActive = true;
            _colorBlindTimer = ColorBlindDuration;

            SendDirectorMessage(ModMessages.ColorBlindStart, ModMessages.ColorBlindStartPlain);
            NetworkManager.GreyAllAndHideNames();
        }

        private static void EndColorBlind()
        {
            _colorBlindActive = false;
            _colorBlindTimer = 0f;

            NetworkManager.RestoreColorsAndNames(_colorBlindOriginal);
            _colorBlindOriginal.Clear();

            SendDirectorMessage(ModMessages.ColorBlindEnd, ModMessages.ColorBlindEndPlain);
        }

        private static void StartFreeze(PlayerControl target)
        {
            if (target?.Data == null || target.Data.IsDead || target.Data.Disconnected) return;

            Vector2 frozenPosition = (Vector2)target.transform.position;
            float originalSpeedMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod);
            _frozenPlayers[target.PlayerId] = (8f, frozenPosition, originalSpeedMod);

            
            SendDirectorMessage(string.Format(ModMessages.FreezeStart, target.Data.PlayerName), string.Format(ModMessages.FreezeStartPlain, target.Data.PlayerName));

            
            if (target == PlayerControl.LocalPlayer) return;

            
            IGameOptions freezeOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            freezeOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.01f);
            Utils.GameOptions.SendGameOptionsToClient(freezeOptions, target.OwnerId);
        }

        private static void EndFreeze(PlayerControl target)
        {
            if (target?.Data == null || !_frozenPlayers.ContainsKey(target.PlayerId)) return;

            bool isHost = target == PlayerControl.LocalPlayer;
            float originalSpeedMod = _frozenPlayers[target.PlayerId].originalSpeed;
            _frozenPlayers.Remove(target.PlayerId);

            
            SendDirectorMessage(string.Format(ModMessages.FreezeEnd, target.Data.PlayerName), string.Format(ModMessages.FreezeEndPlain, target.Data.PlayerName));

            
            if (isHost) return;

            
            IGameOptions normalOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            normalOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, originalSpeedMod);
            Utils.GameOptions.SendGameOptionsToClient(normalOptions, target.OwnerId);
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float dt = Time.deltaTime;

            
            if (_pendingPunishments.Count > 0)
            {
                var playersToPunish = _pendingPunishments.ToArray();
                _pendingPunishments.Clear();
                foreach (var player in playersToPunish)
                {
                    if (player != null && !player.Data.IsDead)
                    {
                        Plugin.Log?.LogInfo($"[DirectorCore] Processing pending punishment for {player.Data.PlayerName}");
                        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
                        {
                            HydraKillPlayer(player);
                        }
                        ChatManager.Queue($"<color=#ff6b6b>{player.Data.PlayerName} a désobéi au script — éliminé !</color>", $"{player.Data.PlayerName} a désobéi au script — éliminé !");
                    }
                }
            }


            ScriptManager.Update();
            Directives.Update(dt);

            
            if (_darknessActive)
            {
                _darknessTimer -= dt;
                if (_darknessTimer <= 0f)
                {
                    EndDarkness();
                }
            }


            if (_colorBlindActive)
            {
                _colorBlindTimer -= dt;
                if (_colorBlindTimer <= 0f)
                {
                    EndColorBlind();
                }
            }

            
            foreach (var kvp in _frozenPlayers.ToList())
            {
                byte playerId = kvp.Key;
                PlayerControl? target = FindById(playerId);

                
                if (target?.Data == null || target.Data.IsDead || target.Data.Disconnected)
                {
                    _frozenPlayers.Remove(playerId);
                    continue;
                }

                
                Vector2 frozenPosition = kvp.Value.position;
                target.transform.position = new Vector3(frozenPosition.x, frozenPosition.y, target.transform.position.z);

                float newTimer = kvp.Value.timer - dt;

                if (newTimer <= 0f)
                {
                    EndFreeze(target);
                }
                else
                {
                    _frozenPlayers[playerId] = (newTimer, frozenPosition, kvp.Value.originalSpeed);
                }
            }

            
            if (_cutActive && ShipStatus.Instance != null)
            {
                _cutTimer -= dt;

                if (_cutPhase == 1) 
                {
                    if (_cutTimer <= 0f)
                    {
                        
                        TriggerReactorSabotage(false);
                        _cutPhase = 2;
                        _cutTimer = 5f;
                    }
                }
                else if (_cutPhase == 2)
                {
                    // Pendant TOUTE la fenêtre d'arrêt : chaque joueur qui bouge est
                    // éliminé (aucune exclusion). On ne s'arrête plus au premier bougeur.
                    foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (pc?.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                        if (_cutKilledPlayers.Contains(pc.PlayerId)) continue;
                        // L'hôte n'est plus épargné : s'il bouge, il meurt comme les autres.

                        if (_cutInitialPositions.TryGetValue(pc.PlayerId, out Vector2 initialPos))
                        {
                            Vector2 currentPos = (Vector2)pc.transform.position;
                            if (Vector2.Distance(initialPos, currentPos) > 0.5f)
                            {
                                _cutKilledPlayers.Add(pc.PlayerId);
                                ChatManager.Queue(string.Format(ModMessages.CutEliminated, pc.Data.PlayerName), string.Format(ModMessages.CutEliminatedPlain, pc.Data.PlayerName));
                                HydraKillPlayer(pc);
                            }
                        }
                    }

                    if (_cutTimer <= 0f)
                    {
                        // Fin de la fenêtre → clôture
                        _cutPhase = 3;
                        _cutTimer = 2f;
                        TriggerReactorSabotage(true);
                    }
                }
                else if (_cutPhase == 3) 
                {
                    if (_cutTimer <= 0f)
                    {
                        
                        TriggerReactorSabotage(false);
                        _cutActive = false;
                        _cutPhase = 0;
                    }
                }
            }

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
                    // On oublie les welcomes déjà envoyés pour que tous les joueurs de retour
                    // au lobby reçoivent le récap GG via le scan de ProcessPendingWelcome.
                    ChatManager.ClearSentWelcome();
                    Plugin.Log?.LogInfo("[DirectorCore] Fin de partie → récap GG délégué au flux welcome.");
                }
            }

            foreach (var k in _cd.Keys.ToList())
                _cd[k] = Mathf.Max(0f, _cd[k] - dt);
        }

        // Wrapper public : permet au module Directives d'envoyer un retour privé au Réalisateur.
        public static void DirectorNotify(string coloredMessage, string plainMessage) => SendDirectorMessage(coloredMessage, plainMessage);

        private static void SendDirectorMessage(string coloredMessage, string plainMessage)
        {
            // Confidentialité : ces retours (confirmations, bannières d'effet) ne vont
            // QU'au Réalisateur, c.-à-d. l'émetteur des commandes director. Si le poste
            // est vacant (aucun mort encore), on retombe sur l'hôte.
            PlayerControl? director = DirectorPlayerId.HasValue ? FindById(DirectorPlayerId.Value) : null;
            PlayerControl? recipient = director ?? PlayerControl.LocalPlayer;
            if (recipient != null)
                ChatManager.QueueSystemMessage(recipient, coloredMessage, plainMessage);
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
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    static class Die_P
    {
        static void Postfix(PlayerControl __instance)
        {
            // Plus de revert anti-cheat : sur serveur privé sans anti-cheat, la mort de
            // l'hôte doit tenir (sinon /cut ne pourrait pas tuer l'hôte).
            DirectorCore.OnPlayerDie(__instance);
            Directives.OnDeath(__instance);
        }
    }

    // Détecte les VRAIS kills (killer != victime) pour l'Ultimatum.
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    static class MurderDetect_P
    {
        static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            if (__instance == null || target == null) return;
            if (__instance.PlayerId == target.PlayerId) return; // self-kill (mod) → ignoré
            Directives.NotifyKill(__instance.PlayerId);
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    static class OnPlayerLeft_P
    {
        static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] InnerNet.ClientData data)
        {
            if (__instance == null || !__instance.AmHost) return;
            DirectorCore.HandlePlayerLeft(data);
        }
    }

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

    // Désactive la fermeture automatique du lobby pour inactivité (compte à rebours ~10 min).
    // Le nom du champ varie selon les versions d'Among Us : on le retrouve par réflexion
    // (propriété float dont le nom contient "Inactiv") et on le maintient à 600 chaque frame.
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    static class NoLobbyTimeout_P
    {
        static System.Reflection.PropertyInfo _prop;
        static bool _init;

        static void Postfix(GameStartManager __instance)
        {
            try
            {
                if (!_init)
                {
                    _init = true;
                    foreach (var pr in typeof(GameStartManager).GetProperties())
                    {
                        if (pr.PropertyType == typeof(float) && pr.CanWrite &&
                            pr.Name.IndexOf("Inactiv", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _prop = pr;
                            Plugin.Log?.LogInfo($"[NoLobbyTimeout] Champ d'inactivité trouvé : {pr.Name}");
                            break;
                        }
                    }
                    if (_prop == null)
                        Plugin.Log?.LogWarning("[NoLobbyTimeout] Aucun champ d'inactivité trouvé sur GameStartManager (timer lobby non neutralisé).");
                }
                _prop?.SetValue(__instance, 600f, null);
            }
            catch (Exception e) { Plugin.Log?.LogError($"[NoLobbyTimeout] {e.Message}"); }
        }
    }

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
            
            
            if (MeetingHud.Instance != null && AmongUsClient.Instance.AmHost)
            {
                foreach (var playerState in MeetingHud.Instance.playerStates)
                {
                    byte playerId = playerState.TargetPlayerId;
                    byte currentVotedFor = playerState.VotedFor;
                    
                    
                    if (!ScriptManager.LastKnownVotedFor.TryGetValue(playerId, out byte lastVotedFor))
                    {
                        lastVotedFor = byte.MaxValue;
                    }
                    
                    
                    if (lastVotedFor == byte.MaxValue && currentVotedFor != byte.MaxValue)
                    {
                        Plugin.Log?.LogInfo($"[VoteTracker] Player {playerId} just voted for {currentVotedFor}!");
                        
                        
                        if (!ScriptManager.VotedPlayerIdsInOrder.Contains(playerId))
                        {
                            ScriptManager.VotedPlayerIdsInOrder.Add(playerId);
                            Plugin.Log?.LogInfo($"[VoteTracker] Added to ordered list, now: [{string.Join(",", ScriptManager.VotedPlayerIdsInOrder)}]");
                        }
                        
                        
                        if (ScriptManager.VoteFirstTargetPlayerId.HasValue)
                        {
                            if (playerId == ScriptManager.VoteFirstTargetPlayerId.Value)
                            {
                                if (!ScriptManager.SomeoneVotedBeforeVoteFirst)
                                {
                                    ScriptManager.VoteFirstTargetVoted = true;
                                    Plugin.Log?.LogInfo($"[VoteTracker] VoteFirst target voted first!");
                                }
                                else
                                {
                                    Plugin.Log?.LogInfo($"[VoteTracker] VoteFirst target voted but someone already went first!");
                                }
                            }
                            else
                            {
                                if (!ScriptManager.VoteFirstTargetVoted)
                                {
                                    ScriptManager.SomeoneVotedBeforeVoteFirst = true;
                                    Plugin.Log?.LogInfo($"[VoteTracker] Someone else voted before VoteFirst target!");
                                }
                            }
                        }
                    }
                    
                    
                    ScriptManager.LastKnownVotedFor[playerId] = currentVotedFor;
                }
            }
        }
    }



    
    // [SUPPRIMÉ] HydraForceDTLS : forçait dtls=true sur chaque appel à
    // InnerNetClient.SetEndpoint, ce qui écrasait le réglage natif du jeu et
    // faisait échouer la négociation DTLS à la création d'un lobby
    // ("DTLS negotiation failed after 35 resends"). On laisse Among Us gérer
    // lui-même le transport.

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
            IGameOptions options = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            options.SetFloat(FloatOptionNames.ShapeshifterCooldown, 0.0f);
            Utils.GameOptions.SendGameOptionsToClient(options, player.OwnerId);
        }
    }

    
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    static class ReportDeadBody_P
    {
        static bool Prefix(PlayerControl __instance)
        {
            Plugin.Log?.LogInfo($"[ReportDeadBody_P] ReportDeadBody called by PlayerId: {__instance.PlayerId}, Name: {__instance.Data.PlayerName}");
            if (!AmongUsClient.Instance.AmHost) 
            {
                Plugin.Log?.LogInfo("[ReportDeadBody_P] Not host - returning true");
                return true;
            }
            
            bool hasScript = ScriptManager.HasScript(__instance.PlayerId, ScriptOrder.NoReport);
            Plugin.Log?.LogInfo($"[ReportDeadBody_P] Has NoReport script: {hasScript}");
            
            if (hasScript)
            {
                Plugin.Log?.LogInfo($"[ReportDeadBody_P] Punishing player for reporting!");
                
                ScriptManager.PunishPlayer(__instance);
                return false; 
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    static class ScriptVote_P
    {
        static void Postfix(int srcClient, int clientId)
        {
            Plugin.Log?.LogInfo($"[ScriptVote_P] PATCH TRIGGERED! srcClient: {srcClient}, clientId: {clientId}, AmHost: {AmongUsClient.Instance?.AmHost}");
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            
            
            PlayerControl? voter = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.OwnerId == srcClient)
                {
                    voter = pc;
                    Plugin.Log?.LogInfo($"[ScriptVote_P] Found voter: {voter.Data.PlayerName} (PlayerId: {voter.PlayerId}, OwnerId: {voter.OwnerId})");
                    break;
                }
            }
            
            if (voter != null)
            {
                
                bool isSkipVote = clientId == 0 || clientId == 253 || clientId == byte.MaxValue;
                Plugin.Log?.LogInfo($"[ScriptVote_P] Vote details - srcClient: {srcClient}, clientId: {clientId}, isSkipVote: {isSkipVote}, voterId: {voter.PlayerId}");

                
                if (!ScriptManager.VotedPlayerIdsInOrder.Contains(voter.PlayerId))
                {
                    ScriptManager.VotedPlayerIdsInOrder.Add(voter.PlayerId);
                    Plugin.Log?.LogInfo($"[ScriptVote_P] Added voter {voter.PlayerId} to ordered list, current list: {string.Join(",", ScriptManager.VotedPlayerIdsInOrder)}");
                }

                
                if (ScriptManager.VoteFirstTargetPlayerId.HasValue)
                {
                    Plugin.Log?.LogInfo($"[ScriptVote_P] VoteFirst target is PlayerId: {ScriptManager.VoteFirstTargetPlayerId.Value}");
                    if (voter.PlayerId == ScriptManager.VoteFirstTargetPlayerId.Value)
                    {
                        
                        if (ScriptManager.SomeoneVotedBeforeVoteFirst)
                        {
                            Plugin.Log?.LogInfo($"[ScriptVote_P] {voter.Data.PlayerName} voted but someone already voted before them - marking as failed");
                        }
                        else
                        {
                            Plugin.Log?.LogInfo($"[ScriptVote_P] {voter.Data.PlayerName} voted first (even skip!) - success!");
                            ScriptManager.VoteFirstTargetVoted = true;
                        }
                    }
                    else
                    {
                        Plugin.Log?.LogInfo($"[ScriptVote_P] {voter.Data.PlayerName} voted - not VoteFirst target");
                        
                        if (!ScriptManager.VoteFirstTargetVoted)
                        {
                            
                            ScriptManager.SomeoneVotedBeforeVoteFirst = true;
                            Plugin.Log?.LogInfo($"[ScriptVote_P] Marked SomeoneVotedBeforeVoteFirst as true");
                        }
                    }
                }
            }
            else
            {
                Plugin.Log?.LogInfo($"[ScriptVote_P] Could not find voter for srcClient: {srcClient}");
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    static class MeetingStart_P
    {
        static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            
            // Marquer les ordres /loc, /action comme complétés
            ScriptManager.CompleteAllScriptsAtMeeting();
            
            // Réinitialiser le tracking VoteFirst
            ScriptManager.ResetVoteFirstTracking();
            Directives.OnMeetingStart();
            Plugin.Log?.LogInfo("[DirectorCore] Meeting started - Scripts completed, VoteFirst tracking reset");
        }
    }
    
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    static class MeetingClose_P
    {
        static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            // Directives liées à la fin de réunion (éjection scriptée, activation Stalker)
            Directives.OnMeetingClose();

            Plugin.Log?.LogInfo("[MeetingClose_P] Meeting closed - checking vote scripts");
            
            
            if (MeetingHud.Instance != null)
            {
                foreach (var playerState in MeetingHud.Instance.playerStates)
                {
                    bool didVote = playerState.VotedFor != byte.MaxValue; 
                    Plugin.Log?.LogInfo($"[MeetingClose_P] PlayerState: PlayerId: {playerState.TargetPlayerId}, DidVote: {didVote}, VotedFor: {playerState.VotedFor}");
                    
                    
                    var allScripts = ScriptManager.GetAllActiveScripts();
                    var playerScript = allScripts.FirstOrDefault(s => s.Key == playerState.TargetPlayerId);
                    if (playerScript.Value != null)
                    {
                        Plugin.Log?.LogInfo($"[MeetingClose_P] Found active script for PlayerId {playerState.TargetPlayerId}: {playerScript.Value.Order}");
                        
                        if (playerScript.Value.Order == ScriptOrder.SkipVote)
                        {
                            
                            
                            bool isSkip = playerState.VotedFor == 0 || playerState.VotedFor == 253 || playerState.VotedFor == byte.MaxValue;
                            bool votedForSomeone = didVote && !isSkip;
                            if (votedForSomeone)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player {playerState.TargetPlayerId} voted for someone (VotedFor: {playerState.VotedFor}) - PUNISHING!");
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null && !player.Data.IsDead)
                                {
                                    ScriptManager.PunishPlayer(player);
                                }
                            }
                            else
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player {playerState.TargetPlayerId} skipped/abstained (VotedFor: {playerState.VotedFor}) - SUCCESS!");
                                
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null)
                                {
                                    ScriptManager.AnnounceSuccess(player);
                                }
                            }
                            ScriptManager.RemoveScript(playerState.TargetPlayerId);
                        }
                        else if (playerScript.Value.Order == ScriptOrder.VoteFirst)
                        {
                            
                            bool success = ScriptManager.VoteFirstTargetVoted && !ScriptManager.SomeoneVotedBeforeVoteFirst;

                            
                            if (!success && ScriptManager.VotedPlayerIdsInOrder.Count > 0)
                            {
                                byte firstVoterId = ScriptManager.VotedPlayerIdsInOrder[0];
                                Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst fallback: first voter in tracked list is {firstVoterId}, target is {playerState.TargetPlayerId}");
                                if (firstVoterId == playerState.TargetPlayerId)
                                {
                                    success = true;
                                    Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst target was first in tracked list - SUCCESS!");
                                }
                            }

                            
                            if (!success)
                            {
                                int targetVoteCount = 0;
                                foreach (var state in MeetingHud.Instance.playerStates)
                                {
                                    if (state.TargetPlayerId == playerState.TargetPlayerId && state.VotedFor != byte.MaxValue)
                                    {
                                        targetVoteCount++;
                                    }
                                }
                                if (targetVoteCount > 0)
                                {
                                    int totalVoters = 0;
                                    foreach (var state in MeetingHud.Instance.playerStates)
                                    {
                                        if (state.VotedFor != byte.MaxValue)
                                        {
                                            totalVoters++;
                                        }
                                    }
                                    if (totalVoters == 1)
                                    {
                                        Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst target was only voter - SUCCESS!");
                                        success = true;
                                    }
                                }
                            }
                            
                            
                            if (!success && ScriptManager.VotedPlayerIdsInOrder.Count == 0)
                            {
                                bool targetVoted = playerState.VotedFor != byte.MaxValue;
                                if (targetVoted)
                                {
                                    Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst target voted & no tracking data - using absolute last resort - SUCCESS!");
                                    success = true;
                                }
                            }

                            if (success)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst target voted first - SUCCESS!");
                                
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null)
                                {
                                    ScriptManager.AnnounceSuccess(player);
                                }
                            }
                            else
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst target didn't vote first - PUNISHING! (VoteFirstTargetVoted: {ScriptManager.VoteFirstTargetVoted}, SomeoneVotedBefore: {ScriptManager.SomeoneVotedBeforeVoteFirst}, trackedList: [{string.Join(",", ScriptManager.VotedPlayerIdsInOrder)}])");
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null && !player.Data.IsDead)
                                {
                                    ScriptManager.PunishPlayer(player);
                                }
                            }
                            ScriptManager.RemoveScript(playerState.TargetPlayerId);
                        }
                        else if (playerScript.Value.Order == ScriptOrder.VoteForPlayer && playerScript.Value.TargetVotePlayerId.HasValue)
                        {
                            byte targetId = playerScript.Value.TargetVotePlayerId.Value;
                            Plugin.Log?.LogInfo($"[MeetingClose_P] VoteForPlayer target: {targetId}, actual vote: {playerState.VotedFor}");
                            if (!didVote)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player didn't vote - PUNISHING!");
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null && !player.Data.IsDead)
                                {
                                    ScriptManager.PunishPlayer(player);
                                }
                            }
                            else if (playerState.VotedFor != targetId)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player voted wrong - PUNISHING!");
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null && !player.Data.IsDead)
                                {
                                    ScriptManager.PunishPlayer(player);
                                }
                            }
                            else
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player voted correctly - SUCCESS!");
                            }
                            ScriptManager.RemoveScript(playerState.TargetPlayerId);
                        }
                    }
                }
            }
            
            
            foreach (var kvp in ScriptManager.GetAllActiveScripts().ToList())
            {
                if (kvp.Value.Order == ScriptOrder.SkipVote || 
                    kvp.Value.Order == ScriptOrder.VoteFirst || 
                    kvp.Value.Order == ScriptOrder.VoteForPlayer)
                {
                    Plugin.Log?.LogInfo($"[MeetingClose_P] Cleaning up remaining script for PlayerId {kvp.Key}: {kvp.Value.Order} - PUNISHING!");
                    var player = DirectorCore.FindById(kvp.Key);
                    if (player != null && !player.Data.IsDead)
                    {
                        ScriptManager.PunishPlayer(player);
                    }
                    ScriptManager.RemoveScript(kvp.Key);
                }
            }
            
            
            ScriptManager.ResetVoteFirstTracking();

            
            try
            {
                if (DevModeManager.devMode && !DevModeManager.endGame)
                {
                    Plugin.Log?.LogInfo("[DirectorCore] DevMode active - ensuring game resumes properly after meeting");
                    
                    if (ShipStatus.Instance != null)
                    {
                        ShipStatus.Instance.enabled = true;
                        ShipStatus.Instance.gameObject.SetActive(true);
                    }
                    
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.enabled = true;
                        GameManager.Instance.gameObject.SetActive(true);
                    }

                    if (HudManager.Instance != null)
                    {
                        HudManager.Instance.gameObject.SetActive(true);
                        HudManager.Instance.enabled = true;
                    }

                    
                    foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (pc?.Data != null && !pc.Data.Disconnected)
                        {
                            pc.gameObject.SetActive(true);
                            pc.NetTransform.enabled = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogError($"[DirectorCore] Error during meeting close: {e.Message}");
            }
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
    static class VentUsePatch
    {
        static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            
            Plugin.Log?.LogInfo($"[VentUsePatch] HandleRpc called with callId: {callId} (RpcCalls: {(RpcCalls)callId})");
            
            
            if ((RpcCalls)callId != RpcCalls.EnterVent && (RpcCalls)callId != RpcCalls.ExitVent) 
            {
                Plugin.Log?.LogInfo($"[VentUsePatch] Not a vent RPC - returning true");
                return true; 
            }
            
            
            PlayerControl player = __instance.myPlayer;
            Plugin.Log?.LogInfo($"[VentUsePatch] Player attempting vent: {player?.Data.PlayerName} (PlayerId: {player?.PlayerId})");
            
            if (player != null && ScriptManager.HasScript(player.PlayerId, ScriptOrder.DontUseVents))
            {
                Plugin.Log?.LogInfo($"[VentUsePatch] {player.Data.PlayerName} has DontUseVents script - PUNISHING!");
                
                DirectorCore.AddPendingPunishment(player);
                ScriptManager.RemoveScript(player.PlayerId);
                return false; 
            }
            
            Plugin.Log?.LogInfo($"[VentUsePatch] {player?.Data.PlayerName} has no DontUseVents script - allowing vent");
            return true;
        }
    }
    

}
