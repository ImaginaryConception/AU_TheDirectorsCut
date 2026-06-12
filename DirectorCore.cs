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
        };

        private static Dictionary<byte, (float timer, Vector2 position, float originalSpeed)> _frozenPlayers = new();
        private static System.Collections.Generic.List<PlayerControl> _pendingPunishments = new();

        private static bool _darknessActive = false;
        private static float _darknessTimer = 0f;
        private static float _originalCrewLightMod = 1f;
        private static float _originalImpostorLightMod = 1f;

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
            _frozenPlayers.Clear();
            _pendingPunishments.Clear();
            ScriptManager.Reset();
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

        private static void SendPrivateMessage(PlayerControl target, string message)
        {
            if (target == null || target.OwnerId < 0) return;
            
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null) return;
            
            try
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    speaker.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, target.OwnerId);
                writer.Write(ChatManager.SafeChat(message));
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                
                Plugin.Log?.LogInfo($"[DirectorCore] Message privé envoyé à {target.Data.PlayerName}: {message}");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogError($"[DirectorCore] Erreur envoi message privé: {e.Message}");
            }
        }

        private static bool TryCheckCooldown(string cmd)
        {
            if (IsOnCooldown(cmd))
            {
                int remaining = Mathf.CeilToInt(CooldownRemaining(cmd));
                string remainingStr = NumberToFrench(remaining);
                SendHostMessage(string.Format(ModMessages.CooldownMsg, cmd, remainingStr));
                return false;
            }
            return true;
        }

        private static void SetCooldown(string cmd)
        {
            if (_cdMax.TryGetValue(cmd, out float max)) _cd[cmd] = max;
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

                case "/join":
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

                case "/hrandomcolors":
                case "/hrandomcolor":
                case "/handomcolors":
                case "/hrandom":
                    ChatManager.Queue(ModMessages.HelpRandomColors, ModMessages.HelpRandomColorsPlain);
                    return true;
                case "/hcut":
                    ChatManager.Queue(ModMessages.HelpCut, ModMessages.HelpCutPlain);
                    return true;
                case "/hdarkness":
                case "/hdark":
                    ChatManager.Queue(ModMessages.HelpDarkness, ModMessages.HelpDarknessPlain);
                    return true;
                case "/hfreeze":
                    ChatManager.Queue(ModMessages.HelpFreeze, ModMessages.HelpFreezePlain);
                    return true;
                case "/haction":
                    ChatManager.QueueSlow(ModMessages.HelpAction, ModMessages.HelpActionPlain);
                    ChatManager.QueueSlow(ModMessages.ActionList, ModMessages.ActionListPlain);
                    return true;
                case "/helpaction":
                    if (parts.Length == 2)
                    {
                        // Specific script help requested
                        if (TryParseScriptLetter(parts[1], out ScriptOrder order))
                        {
                            switch (order)
                            {
                                case ScriptOrder.NoReport:
                                    ChatManager.QueueSlow(ModMessages.HelpActionA, ModMessages.HelpActionAPlain);
                                    break;
                                case ScriptOrder.SkipVote:
                                    ChatManager.QueueSlow(ModMessages.HelpActionB, ModMessages.HelpActionBPlain);
                                    break;
                                case ScriptOrder.DontUseVents:
                                    ChatManager.QueueSlow(ModMessages.HelpActionC, ModMessages.HelpActionCPlain);
                                    break;
                                case ScriptOrder.VoteFirst:
                                    ChatManager.QueueSlow(ModMessages.HelpActionD, ModMessages.HelpActionDPlain);
                                    break;
                            }
                        }
                        else
                        {
                            ChatManager.QueueSlow(ModMessages.UsageAction, ModMessages.UsageActionPlain);
                            ChatManager.QueueSlow(ModMessages.ActionList, ModMessages.ActionListPlain);
                        }
                    }
                    else
                    {
                        // Full help
                        ChatManager.QueueSlow(ModMessages.HelpActionTitle, ModMessages.HelpActionTitlePlain);
                        ChatManager.QueueSlow(ModMessages.HelpActionA, ModMessages.HelpActionAPlain);
                        ChatManager.QueueSlow(ModMessages.HelpActionB, ModMessages.HelpActionBPlain);
                        ChatManager.QueueSlow(ModMessages.HelpActionC, ModMessages.HelpActionCPlain);
                        ChatManager.QueueSlow(ModMessages.HelpActionD, ModMessages.HelpActionDPlain);
                    }
                    return true;
                case "/hloc":
                case "/hollow":
                    ChatManager.QueueSlow(ModMessages.HelpLoc, ModMessages.HelpLocPlain);
                    ChatManager.QueueSlow(ModMessages.LocList1, ModMessages.LocList1Plain);
                    ChatManager.QueueSlow(ModMessages.LocList2, ModMessages.LocList2Plain);
                    return true;
                case "/hvote":
                    ChatManager.Queue(ModMessages.HelpVote, ModMessages.HelpVotePlain);
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
                case "/randomcolors":
                    if (MeetingHud.Instance != null)
                    {
                        SendHostMessage("Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/randomcolors")) return true;
                    SendHostMessage(ModMessages.RandomColorsStart, ModMessages.RandomColorsStartPlain);
                    NetworkManager.RandomizeColors();
                    SetCooldown("/randomcolors");
                    return true;

                case "/cut":
                    if (MeetingHud.Instance != null)
                    {
                        SendHostMessage("Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/cut")) return true;
                    StartCutSequence();
                    SetCooldown("/cut");
                    return true;

                case "/darkness":
                    if (MeetingHud.Instance != null)
                    {
                        SendHostMessage("Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/darkness")) return true;
                    StartDarkness();
                    SetCooldown("/darkness");
                    return true;

                case "/freeze":
                    if (MeetingHud.Instance != null)
                    {
                        SendHostMessage("Cette commande ne peut être utilisée qu'en jeu, pas en réunion !", "Cette commande ne peut être utilisée qu'en jeu, pas en réunion !");
                        return true;
                    }
                    if (!TryCheckCooldown("/freeze")) return true;
                    if (parts.Length < 2)
                    {
                        SendHostMessage("Usage : /freeze ID");
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte freezeTargetId))
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? freezeTarget = FindById(freezeTargetId);
                    if (freezeTarget?.Data == null)
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (_frozenPlayers.ContainsKey(freezeTarget.PlayerId))
                    {
                        SendHostMessage($"{freezeTarget.Data.PlayerName} est déjà bloqué !");
                        return true;
                    }
                    StartFreeze(freezeTarget);
                    SetCooldown("/freeze");
                    return true;

                case "/action":
                    if (MeetingHud.Instance == null)
                    {
                        SendHostMessage(ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/action")) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSlow(ModMessages.UsageAction, ModMessages.UsageActionPlain);
                        ChatManager.QueueSlow(ModMessages.ActionList, ModMessages.ActionListPlain);
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte actionTargetId))
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? actionTarget = FindById(actionTargetId);
                    if (actionTarget?.Data == null)
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (parts.Length < 3)
                    {
                        // Just show the script list to the director
                        ChatManager.QueueSlow(ModMessages.UsageAction, ModMessages.UsageActionPlain);
                        ChatManager.QueueSlow(ModMessages.ActionList, ModMessages.ActionListPlain);
                        return true;
                    }
                    
                    if (!TryParseScriptLetter(parts[2], out ScriptOrder order))
                    {
                        ChatManager.QueueSlow(ModMessages.UsageAction, ModMessages.UsageActionPlain);
                        ChatManager.QueueSlow(ModMessages.ActionList, ModMessages.ActionListPlain);
                        return true;
                    }
                    
                    // Only allow the basic scripts here
                    if (order == ScriptOrder.StayOut || order == ScriptOrder.VoteForPlayer)
                    {
                        SendHostMessage("Utilisez /loc ou /vote pour ces ordres !", "Utilisez /loc ou /vote pour ces ordres !");
                        return true;
                    }
                    
                    if (ScriptManager.HasScript(actionTarget.PlayerId))
                    {
                        SendHostMessage(string.Format(ModMessages.ActionAlreadyActive, actionTarget.Data.PlayerName), string.Format(ModMessages.ActionAlreadyActivePlain, actionTarget.Data.PlayerName));
                        return true;
                    }
                    
                    // Assign the script
                    ScriptManager.AssignScript(actionTarget.PlayerId, order);
                    
                    // Send public message to everyone
                    var (plainMsg, coloredMsg) = ScriptManager.GetOrderPrivateMessages(order, actionTarget.Data.PlayerName);
                    ChatManager.QueueSlow(coloredMsg, plainMsg);
                    
                    // Now set cooldown
                    SetCooldown("/action");
                    return true;
                    
                case "/loc":
                    if (MeetingHud.Instance == null)
                    {
                        SendHostMessage(ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/loc")) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSlow(ModMessages.UsageLoc, ModMessages.UsageLocPlain);
                        ChatManager.QueueSlow(ModMessages.LocList1, ModMessages.LocList1Plain);
                        ChatManager.QueueSlow(ModMessages.LocList2, ModMessages.LocList2Plain);
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte locTargetId))
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? locTarget = FindById(locTargetId);
                    if (locTarget?.Data == null)
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (parts.Length < 3)
                    {
                        // Show location list
                        ChatManager.QueueSlow(ModMessages.UsageLoc, ModMessages.UsageLocPlain);
                        ChatManager.QueueSlow(ModMessages.LocList1, ModMessages.LocList1Plain);
                        ChatManager.QueueSlow(ModMessages.LocList2, ModMessages.LocList2Plain);
                        return true;
                    }
                    
                    if (!TryParseZoneLetter(parts[2], out MapLocation location))
                    {
                        ChatManager.QueueSlow(ModMessages.UsageLoc, ModMessages.UsageLocPlain);
                        ChatManager.QueueSlow(ModMessages.LocList1, ModMessages.LocList1Plain);
                        ChatManager.QueueSlow(ModMessages.LocList2, ModMessages.LocList2Plain);
                        return true;
                    }
                    
                    if (ScriptManager.HasScript(locTarget.PlayerId))
                    {
                        SendHostMessage(string.Format(ModMessages.ActionAlreadyActive, locTarget.Data.PlayerName), string.Format(ModMessages.ActionAlreadyActivePlain, locTarget.Data.PlayerName));
                        return true;
                    }
                    
                    // Assign the script
                    ScriptManager.AssignStayOutScript(locTarget.PlayerId, location);
                    
                    // Send public message to everyone
                    var (locPlain, locColored) = ScriptManager.GetStayOutPrivateMessages(location, locTarget.Data.PlayerName);
                    ChatManager.QueueSlow(locColored, locPlain);
                    
                    // Set cooldown
                    SetCooldown("/loc");
                    return true;
                    
                case "/vote":
                    if (MeetingHud.Instance == null)
                    {
                        SendHostMessage(ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/vote")) return true;
                    if (parts.Length < 3)
                    {
                        SendHostMessage(ModMessages.UsageVote, ModMessages.UsageVotePlain);
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte voteTargetId))
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (!byte.TryParse(parts[2], out byte voteForId))
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    PlayerControl? voteTarget = FindById(voteTargetId);
                    PlayerControl? voteForTarget = FindById(voteForId);
                    
                    if (voteTarget?.Data == null || voteForTarget?.Data == null)
                    {
                        SendHostMessage(ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (ScriptManager.HasScript(voteTarget.PlayerId))
                    {
                        SendHostMessage(string.Format(ModMessages.ActionAlreadyActive, voteTarget.Data.PlayerName), string.Format(ModMessages.ActionAlreadyActivePlain, voteTarget.Data.PlayerName));
                        return true;
                    }
                    
                    // Assign the script
                    ScriptManager.AssignVoteForPlayerScript(voteTarget.PlayerId, voteForId);
                    
                    // Send public message to everyone
                    var (votePlain, voteColored) = ScriptManager.GetVoteForPlayerPrivateMessages(voteForTarget.Data.PlayerName, voteTarget.Data.PlayerName);
                    ChatManager.QueueSlow(voteColored, votePlain);
                    
                    // Set cooldown
                    SetCooldown("/vote");
                    return true;

                default:
                    SendHostMessage($"Commande inconnue : {cmd} — /help");
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

            // Announce /cut in chat
            ChatManager.Queue(ModMessages.CutStart, ModMessages.CutStartPlain);

            // Record initial positions of all alive players (except host)
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data != null && !pc.Data.IsDead && !pc.Data.Disconnected && pc != PlayerControl.LocalPlayer)
                {
                    _cutInitialPositions[pc.PlayerId] = (Vector2)pc.transform.position;
                }
            }

            // Trigger reactor sabotage
            TriggerReactorSabotage(true);
        }

        private static void TriggerReactorSabotage(bool active)
        {
            if (ShipStatus.Instance == null) return;

            // Get correct reactor system type for current map
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
                // Start sabotage
                ShipStatus.Instance.RpcUpdateSystem(reactorType, 128);
            }
            else
            {
                // Stop sabotage
                ShipStatus.Instance.RpcUpdateSystem(reactorType, 16);
            }
        }

        private static void HydraKillPlayer(PlayerControl target)
        {
            // Use Hydra's method to kill the player, exactly like in PlayersSection.AttemptMurder and HostSection
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
            {
                // Kill the target (no host position lock/teleport needed!)
                PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
            }
        }

        private static void StartDarkness()
        {
            if (ShipStatus.Instance == null) return;

            // Save original light mod values
            _originalCrewLightMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.CrewLightMod);
            _originalImpostorLightMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);

            _darknessActive = true;
            _darknessTimer = 10f;

            // Announce
            ChatManager.Queue(ModMessages.DarknessStart, ModMessages.DarknessStartPlain);

            // Blind everyone using Hydra's method
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null || pc.Data.Disconnected) continue;

                IGameOptions blindOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                blindOptions.SetFloat(FloatOptionNames.CrewLightMod, -1.0f);
                blindOptions.SetFloat(FloatOptionNames.ImpostorLightMod, -1.0f);
                Hydra.GameOptions.SendGameOptionsToClient(blindOptions, pc.OwnerId);
            }
        }

        private static void EndDarkness()
        {
            if (ShipStatus.Instance == null) return;

            _darknessActive = false;
            _darknessTimer = 0f;

            // Announce
            ChatManager.Queue(ModMessages.DarknessEnd, ModMessages.DarknessEndPlain);

            // Restore normal lighting using Hydra's method with original values
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null || pc.Data.Disconnected) continue;

                IGameOptions normalOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                normalOptions.SetFloat(FloatOptionNames.CrewLightMod, _originalCrewLightMod);
                normalOptions.SetFloat(FloatOptionNames.ImpostorLightMod, _originalImpostorLightMod);
                Hydra.GameOptions.SendGameOptionsToClient(normalOptions, pc.OwnerId);
            }
        }

        private static void StartFreeze(PlayerControl target)
        {
            if (target?.Data == null || target.Data.IsDead || target.Data.Disconnected) return;

            Vector2 frozenPosition = (Vector2)target.transform.position;
            float originalSpeedMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod);
            _frozenPlayers[target.PlayerId] = (8f, frozenPosition, originalSpeedMod);

            // Announce
            ChatManager.Queue(string.Format(ModMessages.FreezeStart, target.Data.PlayerName), string.Format(ModMessages.FreezeStartPlain, target.Data.PlayerName));

            // If target IS the host: ONLY use local position-forcing, DO NOT send per-client options (prevents global options change!)
            if (target == PlayerControl.LocalPlayer) return;

            // Otherwise, for vanilla/other clients: use per-client speed-mod (0.01f) like before
            IGameOptions freezeOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            freezeOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.01f);
            Hydra.GameOptions.SendGameOptionsToClient(freezeOptions, target.OwnerId);
        }

        private static void EndFreeze(PlayerControl target)
        {
            if (target?.Data == null || !_frozenPlayers.ContainsKey(target.PlayerId)) return;

            bool isHost = target == PlayerControl.LocalPlayer;
            float originalSpeedMod = _frozenPlayers[target.PlayerId].originalSpeed;
            _frozenPlayers.Remove(target.PlayerId);

            // Announce
            ChatManager.Queue(string.Format(ModMessages.FreezeEnd, target.Data.PlayerName), string.Format(ModMessages.FreezeEndPlain, target.Data.PlayerName));

            // If target IS the host: no need to send options, just stop forcing position
            if (isHost) return;

            // Otherwise, for vanilla/other clients: restore original speed mod
            IGameOptions normalOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            normalOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, originalSpeedMod);
            Hydra.GameOptions.SendGameOptionsToClient(normalOptions, target.OwnerId);
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float dt = Time.deltaTime;

            // Handle pending punishments first
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

            // Handle ScriptManager updates (StayStill order)
            ScriptManager.Update();

            // Handle /darkness timer
            if (_darknessActive)
            {
                _darknessTimer -= dt;
                if (_darknessTimer <= 0f)
                {
                    EndDarkness();
                }
            }

            // Handle /freeze timers AND force position
            foreach (var kvp in _frozenPlayers.ToList())
            {
                byte playerId = kvp.Key;
                PlayerControl? target = FindById(playerId);

                // If player is dead, disconnected, or not found: remove from list
                if (target?.Data == null || target.Data.IsDead || target.Data.Disconnected)
                {
                    _frozenPlayers.Remove(playerId);
                    continue;
                }

                // Force position without any RPC calls!
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

            // Handle /cut sequence
            if (_cutActive && ShipStatus.Instance != null)
            {
                _cutTimer -= dt;

                if (_cutPhase == 1) // Reactor alert phase (2s)
                {
                    if (_cutTimer <= 0f)
                    {
                        // Stop reactor, start no-movement phase
                        TriggerReactorSabotage(false);
                        _cutPhase = 2;
                        _cutTimer = 5f;
                    }
                }
                else if (_cutPhase == 2) // No-movement phase (5s)
                {
                    // Check all players for movement
                    bool someoneMoved = false;
                    foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (pc?.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                        if (_cutKilledPlayers.Contains(pc.PlayerId)) continue;
                        if (pc == PlayerControl.LocalPlayer) continue; // Skip host!

                        if (_cutInitialPositions.TryGetValue(pc.PlayerId, out Vector2 initialPos))
                        {
                            Vector2 currentPos = (Vector2)pc.transform.position;
                            float distance = Vector2.Distance(initialPos, currentPos);
                            if (distance > 0.5f) // Lower threshold for better movement detection
                            {
                                someoneMoved = true;
                                _cutKilledPlayers.Add(pc.PlayerId);
                                // Announce in chat that the player was eliminated for moving
                                ChatManager.Queue(string.Format(ModMessages.CutEliminated, pc.Data.PlayerName), string.Format(ModMessages.CutEliminatedPlain, pc.Data.PlayerName));
                                HydraKillPlayer(pc);
                            }
                        }
                    }

                    if (someoneMoved)
                    {
                        // Restart reactor alert for 2s
                        _cutPhase = 1;
                        _cutTimer = 2f;
                        TriggerReactorSabotage(true);
                        // Update initial positions (exclude killed players and host)
                        _cutInitialPositions.Clear();
                        foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                        {
                            if (pc?.Data != null && !pc.Data.IsDead && !pc.Data.Disconnected && !_cutKilledPlayers.Contains(pc.PlayerId) && pc != PlayerControl.LocalPlayer)
                            {
                                _cutInitialPositions[pc.PlayerId] = (Vector2)pc.transform.position;
                            }
                        }
                    }
                    else if (_cutTimer <= 0f)
                    {
                        // Start end alert phase
                        _cutPhase = 3;
                        _cutTimer = 2f;
                        TriggerReactorSabotage(true);
                    }
                }
                else if (_cutPhase == 3) // Reactor end alert (2s)
                {
                    if (_cutTimer <= 0f)
                    {
                        // End sequence
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
                    Plugin.Log?.LogInfo("[DirectorCore] Fin de partie → récap délégué au flux welcome.");
                }
            }

            foreach (var k in _cd.Keys.ToList())
                _cd[k] = Mathf.Max(0f, _cd[k] - dt);
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

    // ── SCRIPT MANAGER PATCHES ────────────────────────────────────────────────────
    
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
                // Player disobeyed - kill them!
                ScriptManager.PunishPlayer(__instance);
                return false; // Prevent the report
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    static class ScriptVote_P
    {
        static void Postfix(int srcClient, int clientId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            
            Plugin.Log?.LogInfo($"[ScriptVote_P] AddVote called - srcClient: {srcClient}, clientId: {clientId}");
            
            // Find the player who voted
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
                // Only track VoteFirst logic - let MeetingClose_P handle all other checks/punishments!
                if (ScriptManager.VoteFirstTargetPlayerId.HasValue)
                {
                    Plugin.Log?.LogInfo($"[ScriptVote_P] VoteFirst target is PlayerId: {ScriptManager.VoteFirstTargetPlayerId.Value}");
                    if (voter.PlayerId == ScriptManager.VoteFirstTargetPlayerId.Value)
                    {
                        // They voted! Check if someone already voted before them
                        if (ScriptManager.SomeoneVotedBeforeVoteFirst)
                        {
                            Plugin.Log?.LogInfo($"[ScriptVote_P] {voter.Data.PlayerName} voted but someone already voted before them - marking as failed");
                        }
                        else
                        {
                            Plugin.Log?.LogInfo($"[ScriptVote_P] {voter.Data.PlayerName} voted first - success!");
                            ScriptManager.VoteFirstTargetVoted = true;
                        }
                    }
                    else
                    {
                        Plugin.Log?.LogInfo($"[ScriptVote_P] {voter.Data.PlayerName} voted - not VoteFirst target");
                        // Someone else voted!
                        if (!ScriptManager.VoteFirstTargetVoted)
                        {
                            // Target hasn't voted yet - mark that someone voted before them
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
            
            // Reset VoteFirst tracking at the start of each meeting
            ScriptManager.ResetVoteFirstTracking();
            Plugin.Log?.LogInfo("[DirectorCore] Meeting started - VoteFirst tracking reset");
        }
    }
    
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    static class MeetingClose_P
    {
        static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            
            Plugin.Log?.LogInfo("[MeetingClose_P] Meeting closed - checking vote scripts");
            
            // First, check all player states from MeetingHud to verify voting orders
            if (MeetingHud.Instance != null)
            {
                foreach (var playerState in MeetingHud.Instance.playerStates)
                {
                    bool didVote = playerState.VotedFor != byte.MaxValue; // Assume byte.MaxValue means didn't vote
                    Plugin.Log?.LogInfo($"[MeetingClose_P] PlayerState: PlayerId: {playerState.TargetPlayerId}, DidVote: {didVote}, VotedFor: {playerState.VotedFor}");
                    
                    // Check if this player has an active voting script
                    var allScripts = ScriptManager.GetAllActiveScripts();
                    var playerScript = allScripts.FirstOrDefault(s => s.Key == playerState.TargetPlayerId);
                    if (playerScript.Value != null)
                    {
                        Plugin.Log?.LogInfo($"[MeetingClose_P] Found active script for PlayerId {playerState.TargetPlayerId}: {playerScript.Value.Order}");
                        
                        if (playerScript.Value.Order == ScriptOrder.SkipVote)
                        {
                            if (didVote && playerState.VotedFor != 0)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player {playerState.TargetPlayerId} voted for someone - PUNISHING!");
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null && !player.Data.IsDead)
                                {
                                    ScriptManager.PunishPlayer(player);
                                }
                            }
                            else if (didVote && playerState.VotedFor == 0)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player {playerState.TargetPlayerId} skipped - SUCCESS!");
                            }
                            else
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] Player {playerState.TargetPlayerId} didn't vote at all - PUNISHING!");
                                var player = DirectorCore.FindById(playerState.TargetPlayerId);
                                if (player != null && !player.Data.IsDead)
                                {
                                    ScriptManager.PunishPlayer(player);
                                }
                            }
                            ScriptManager.RemoveScript(playerState.TargetPlayerId);
                        }
                        else if (playerScript.Value.Order == ScriptOrder.VoteFirst)
                        {
                            if (!ScriptManager.VoteFirstTargetVoted || ScriptManager.SomeoneVotedBeforeVoteFirst)
                            {
                                Plugin.Log?.LogInfo($"[MeetingClose_P] VoteFirst target didn't vote first - PUNISHING!");
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
            
            // Clean up any remaining meeting-specific scripts (in case player state wasn't found)
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
            
            // Reset VoteFirst tracking for next meeting
            ScriptManager.ResetVoteFirstTracking();

            // Fix for black screen after meeting with <3 players in dev mode
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

                    // Also ensure all players are properly respawned/active
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
    
    // Patch to ensure the game doesn't transition to game over after exile when in dev mode
    // [HarmonyPatch(typeof(LogicGameFlowNormal), "OnExileEnd")]
    // static class DevMode_OnExileEnd_P
    // {
    //     static bool Prefix()
    //     {
    //         if (DevModeManager.devMode && !DevModeManager.endGame && AmongUsClient.Instance?.AmHost == true)
    //         {
    //             Plugin.Log?.LogInfo("[DevMode] Ensuring game continues after exile!");
    //             // Just let the game resume normally without checking end criteria
    //             return false;
    //         }
    //         return true;
    //     }
    // }
    
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
    static class VentUsePatch
    {
        static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            
            Plugin.Log?.LogInfo($"[VentUsePatch] HandleRpc called with callId: {callId} (RpcCalls: {(RpcCalls)callId})");
            
            // Check if it's an EnterVent or ExitVent RPC
            if ((RpcCalls)callId != RpcCalls.EnterVent && (RpcCalls)callId != RpcCalls.ExitVent) 
            {
                Plugin.Log?.LogInfo($"[VentUsePatch] Not a vent RPC - returning true");
                return true; 
            }
            
            // Get the player who tried to vent
            PlayerControl player = __instance.myPlayer;
            Plugin.Log?.LogInfo($"[VentUsePatch] Player attempting vent: {player?.Data.PlayerName} (PlayerId: {player?.PlayerId})");
            
            if (player != null && ScriptManager.HasScript(player.PlayerId, ScriptOrder.DontUseVents))
            {
                Plugin.Log?.LogInfo($"[VentUsePatch] {player.Data.PlayerName} has DontUseVents script - PUNISHING!");
                // Punish!
                DirectorCore.AddPendingPunishment(player);
                ScriptManager.RemoveScript(player.PlayerId);
                return false; // Prevent the vent use
            }
            
            Plugin.Log?.LogInfo($"[VentUsePatch] {player?.Data.PlayerName} has no DontUseVents script - allowing vent");
            return true;
        }
    }
    

}
