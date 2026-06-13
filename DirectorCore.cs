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
                    ChatManager.QueueSystemMessage(sender, $"Commande inconnue : {cmd} — /help", $"Commande inconnue : {cmd} — /help");
                    return true;
                }

                if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                {
                    ChatManager.QueueSystemMessage(sender, ModMessages.HostOnly, ModMessages.HostOnlyPlain);
                    return true;
                }

                switch (cmd)
                {
                    case "/start":
                        if (!inLobby) { ChatManager.QueueSystemMessage(sender, "Pas en lobby !", "Pas en lobby !"); return true; }
                        AmongUsClient.Instance.StartGame();
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

                    case "/setdirector":
                        if (parts.Length >= 2 && byte.TryParse(parts[1], out byte did))
                        {
                            var dtarget = FindById(did);
                            if (dtarget?.Data == null)
                            {
                                ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                                return true;
                            }
                            DirectorPlayerId = dtarget.PlayerId;
                            DirectorName = dtarget.Data.PlayerName;
                            ChatManager.QueueSystemMessage(sender,
                                string.Format(ModMessages.DirectorSet, dtarget.Data.PlayerName),
                                string.Format(ModMessages.DirectorSetPlain, dtarget.Data.PlayerName)
                            );
                        }
                        else
                        {
                            DirectorPlayerId = sender.PlayerId;
                            DirectorName = sender.Data.PlayerName;
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
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.Help1, ModMessages.Help1Plain);
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.Help2, ModMessages.Help2Plain);
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.Help3, ModMessages.Help3Plain);
                    return true;

                case "/gg":
                    ChatManager.SendPrivateGGToAll();
                    return true;

                case "/join":
                case "/discord":
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.Discord, ModMessages.DiscordPlain);
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
                            ChatManager.QueueSystemMessageSlow(sender, chunksColored[i], chunksPlain[i]);

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
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpAction, ModMessages.HelpActionPlain);
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.ActionList, ModMessages.ActionListPlain);
                    return true;
                case "/helpaction":
                    if (parts.Length == 2)
                    {
                        
                        if (TryParseScriptLetter(parts[1], out ScriptOrder order))
                        {
                            switch (order)
                            {
                                case ScriptOrder.NoReport:
                                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionA, ModMessages.HelpActionAPlain);
                                    break;
                                case ScriptOrder.SkipVote:
                                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionB, ModMessages.HelpActionBPlain);
                                    break;
                                case ScriptOrder.DontUseVents:
                                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionC, ModMessages.HelpActionCPlain);
                                    break;
                                case ScriptOrder.VoteFirst:
                                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionD, ModMessages.HelpActionDPlain);
                                    break;
                            }
                        }
                        else
                        {
                            ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageAction, ModMessages.UsageActionPlain);
                            ChatManager.QueueSystemMessageSlow(sender, ModMessages.ActionList, ModMessages.ActionListPlain);
                        }
                    }
                    else
                    {
                        
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionTitle, ModMessages.HelpActionTitlePlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionA, ModMessages.HelpActionAPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionB, ModMessages.HelpActionBPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionC, ModMessages.HelpActionCPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpActionD, ModMessages.HelpActionDPlain);
                    }
                    return true;
                case "/hloc":
                case "/hollow":
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.HelpLoc, ModMessages.HelpLocPlain);
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList1, ModMessages.LocList1Plain);
                    ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList2, ModMessages.LocList2Plain);
                    return true;
                case "/hvote":
                    ChatManager.QueueSystemMessage(sender, ModMessages.HelpVote, ModMessages.HelpVotePlain);
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
                    ChatManager.QueueSystemMessage(sender, ModMessages.RandomColorsStart, ModMessages.RandomColorsStartPlain);
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
                        ChatManager.QueueSystemMessage(sender, "Usage : /freeze ID", "Usage : /freeze ID");
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte freezeTargetId))
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
                    if (MeetingHud.Instance == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/action", sender)) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageAction, ModMessages.UsageActionPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.ActionList, ModMessages.ActionListPlain);
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte actionTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? actionTarget = FindById(actionTargetId);
                    if (actionTarget?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (parts.Length < 3)
                    {
                        
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageAction, ModMessages.UsageActionPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.ActionList, ModMessages.ActionListPlain);
                        return true;
                    }
                    
                    if (!TryParseScriptLetter(parts[2], out ScriptOrder order))
                    {
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageAction, ModMessages.UsageActionPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.ActionList, ModMessages.ActionListPlain);
                        return true;
                    }
                    
                    
                    if (order == ScriptOrder.StayOut || order == ScriptOrder.VoteForPlayer)
                    {
                        ChatManager.QueueSystemMessage(sender, "Utilisez /loc ou /vote pour ces ordres !", "Utilisez /loc ou /vote pour ces ordres !");
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
                    
                    ChatManager.QueueSystemMessage(sender, string.Format(ModMessages.ActionAssigned, actionTarget.Data.PlayerName), string.Format(ModMessages.ActionAssignedPlain, actionTarget.Data.PlayerName));
                    
                    
                    SetCooldown("/action");
                    return true;
                    
                case "/loc":
                    if (MeetingHud.Instance == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.OnlyInMeeting, ModMessages.OnlyInMeetingPlain);
                        return true;
                    }
                    if (!TryCheckCooldown("/loc", sender)) return true;
                    if (parts.Length < 2)
                    {
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageLoc, ModMessages.UsageLocPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList1, ModMessages.LocList1Plain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList2, ModMessages.LocList2Plain);
                        return true;
                    }
                    if (!byte.TryParse(parts[1], out byte locTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    PlayerControl? locTarget = FindById(locTargetId);
                    if (locTarget?.Data == null)
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    
                    if (parts.Length < 3)
                    {
                        
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageLoc, ModMessages.UsageLocPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList1, ModMessages.LocList1Plain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList2, ModMessages.LocList2Plain);
                        return true;
                    }
                    
                    if (!TryParseZoneLetter(parts[2], out MapLocation location))
                    {
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.UsageLoc, ModMessages.UsageLocPlain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList1, ModMessages.LocList1Plain);
                        ChatManager.QueueSystemMessageSlow(sender, ModMessages.LocList2, ModMessages.LocList2Plain);
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
                    
                    ChatManager.QueueSystemMessage(sender, string.Format(ModMessages.LocAssigned, locTarget.Data.PlayerName), string.Format(ModMessages.LocAssignedPlain, locTarget.Data.PlayerName));
                    
                    
                    SetCooldown("/loc");
                    return true;
                    
                case "/vote":
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
                    if (!byte.TryParse(parts[1], out byte voteTargetId))
                    {
                        ChatManager.QueueSystemMessage(sender, ModMessages.PlayerNotFound, ModMessages.PlayerNotFoundPlain);
                        return true;
                    }
                    if (!byte.TryParse(parts[2], out byte voteForId))
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
                    
                    ChatManager.QueueSystemMessage(sender, string.Format(ModMessages.VoteAssigned, voteTarget.Data.PlayerName), string.Format(ModMessages.VoteAssignedPlain, voteTarget.Data.PlayerName));
                    
                    
                    SetCooldown("/vote");
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

            
            ChatManager.QueueToDirectorAndHost(ModMessages.CutStart, ModMessages.CutStartPlain);

            
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data != null && !pc.Data.IsDead && !pc.Data.Disconnected && pc != PlayerControl.LocalPlayer)
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
            
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
            {
                if (target == PlayerControl.LocalPlayer)
                {
                    // For the host, call Die() directly instead of RpcMurderPlayer
                    target.Die(DeathReason.Kill, true);
                }
                else
                {
                    // For other players, use RpcMurderPlayer
                    PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
                }
            }
        }

        private static void StartDarkness()
        {
            if (ShipStatus.Instance == null) return;

            
            _originalCrewLightMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.CrewLightMod);
            _originalImpostorLightMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);

            _darknessActive = true;
            _darknessTimer = 10f;

            
            ChatManager.QueueToDirectorAndHost(ModMessages.DarknessStart, ModMessages.DarknessStartPlain);

            
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

            
            ChatManager.QueueToDirectorAndHost(ModMessages.DarknessEnd, ModMessages.DarknessEndPlain);

            
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

        private static void StartFreeze(PlayerControl target)
        {
            if (target?.Data == null || target.Data.IsDead || target.Data.Disconnected) return;

            Vector2 frozenPosition = (Vector2)target.transform.position;
            float originalSpeedMod = GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod);
            _frozenPlayers[target.PlayerId] = (8f, frozenPosition, originalSpeedMod);

            
            ChatManager.QueueToDirectorAndHost(string.Format(ModMessages.FreezeStart, target.Data.PlayerName), string.Format(ModMessages.FreezeStartPlain, target.Data.PlayerName));

            
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

            
            ChatManager.QueueToDirectorAndHost(string.Format(ModMessages.FreezeEnd, target.Data.PlayerName), string.Format(ModMessages.FreezeEndPlain, target.Data.PlayerName));

            
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

            
            if (_darknessActive)
            {
                _darknessTimer -= dt;
                if (_darknessTimer <= 0f)
                {
                    EndDarkness();
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
                    
                    PlayerControl? firstMoved = null;
                    foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (pc?.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                        if (_cutKilledPlayers.Contains(pc.PlayerId)) continue;
                        if (pc == PlayerControl.LocalPlayer) continue; 

                        if (_cutInitialPositions.TryGetValue(pc.PlayerId, out Vector2 initialPos))
                        {
                            Vector2 currentPos = (Vector2)pc.transform.position;
                            float distance = Vector2.Distance(initialPos, currentPos);
                            if (distance > 0.5f) 
                            {
                                firstMoved = pc;
                                break;
                            }
                        }
                    }

                    if (firstMoved != null)
                    {
                        _cutKilledPlayers.Add(firstMoved.PlayerId);
                        ChatManager.Queue(string.Format(ModMessages.CutEliminated, firstMoved.Data.PlayerName), string.Format(ModMessages.CutEliminatedPlain, firstMoved.Data.PlayerName));
                        HydraKillPlayer(firstMoved);
                        
                        _cutPhase = 3;
                        _cutTimer = 2f;
                        TriggerReactorSabotage(true);
                    }
                    else if (_cutTimer <= 0f)
                    {
                        
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
