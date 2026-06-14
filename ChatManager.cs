using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    public static class ChatManager
    {
        internal static bool IsSending = false;

        
        internal static readonly Dictionary<string, string> _colorMap = new();

        
        
        
        private static readonly Queue<(string plain, string colored, float wait, PlayerControl? target)> _queue = new();
        private const int MaxQueueSize = 20; 

        
        private const int MaxChatChars = 100;
        private const int MaxChatBytes = 100;
        public static string SafeChat(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length > MaxChatChars) s = s.Substring(0, MaxChatChars - 3) + "...";
            
            
            
            while (System.Text.Encoding.UTF8.GetByteCount(s) > MaxChatBytes && s.Length > 1)
                s = s.Substring(0, s.Length - 1);
            return s;
        }

        
        public static void SendPrivate(PlayerControl target, string plainMessage)
        {
            if (target == null || target.OwnerId < 0) return;
            SendPrivate(target, plainMessage, plainMessage);
        }



        
        private static void SendPrivate(PlayerControl target, string plainMsg, string coloredMsg)
        {
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null || target == null || target.OwnerId < 0) return;
            try
            {
                _colorMap[plainMsg] = coloredMsg;
                
                // Afficher localement uniquement si le message est pour nous
                if (target == PlayerControl.LocalPlayer)
                {
                    try
                    {
                        IsSending = true;
                        HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                        IsSending = false;
                    }
                    catch { IsSending = false; }
                }
                else
                {
                    // Envoyer le message via réseau au destinataire
                    const string sysName = "System";
                    string orig = speaker.Data.PlayerName;

                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WSetName(w, speaker, sysName);
                    WSendChat(w, speaker, SafeChat(plainMsg));
                    WSetName(w, speaker, orig);
                    w.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(w);
                    w.Recycle();
                }
                
                Plugin.Log?.LogInfo($"[ChatManager] Message envoyé (privé) → {target.Data.PlayerName}: {plainMsg}");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[SendPrivate] {e.Message}"); }
        }

        
        public static void SendPrivateScript(PlayerControl target, string plainMsg, string coloredMsg)
        {
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null || target == null) return;
            bool inLobby = ShipStatus.Instance == null;
            bool inMeeting = MeetingHud.Instance != null;
            
            try
            {
                
                // Afficher localement uniquement si le message est pour nous
                if (target == PlayerControl.LocalPlayer)
                {
                    try
                    {
                        IsSending = true;
                        HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                        IsSending = false;
                    }
                    catch { IsSending = false; }
                }
                
                _colorMap[plainMsg] = coloredMsg;
                
                if (inLobby || inMeeting)
                {
                    
                    SendPrivate(target, plainMsg, coloredMsg);
                }
                else
                {
                    
                    const string sysName = "Director";
                    string orig = speaker.Data.PlayerName;

                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6); 
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId); 
                    WSetName(w, speaker, sysName);
                    WSendChat(w, speaker, SafeChat(plainMsg));
                    WSetName(w, speaker, orig);
                    w.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(w);
                    w.Recycle();
                }
                
                Plugin.Log?.LogInfo($"[ChatManager] Message de script envoyé (privé) → {target.Data.PlayerName}: {plainMsg}");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[SendPrivateScript] {e.Message}"); }
        }

        
        
        
        public static void ShowHostLocal(string coloredMsg, string plainMsg)
        {
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null || HudManager.Instance?.Chat == null) return;
            try
            {
                _colorMap[plainMsg] = coloredMsg;
                IsSending = true;
                HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                IsSending = false;
            }
            catch (Exception e) { IsSending = false; Plugin.Log?.LogError($"[ShowHostLocal] {e.Message}"); }
        }

        public static void Queue(string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg))
            {
                if (_queue.Count >= MaxQueueSize) return; 
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg, -1f, null)); 
            }
        }

        
        public static void Queue(string coloredMsg)
        {
            string plainMsg = System.Text.RegularExpressions.Regex.Replace(coloredMsg, "<[^>]*>", "");
            Queue(coloredMsg, plainMsg);
        }

        
        public static void QueueSlow(string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg))
            {
                if (_queue.Count >= MaxQueueSize) return; 
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg, 3.5f, null));
            }
        }

        public static void QueueSystemMessage(PlayerControl target, string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg) && target != null)
            {
                if (_queue.Count >= MaxQueueSize) return;
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg, -1f, target));
            }
        }

        public static void QueueSystemMessageSlow(PlayerControl target, string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg) && target != null)
            {
                if (_queue.Count >= MaxQueueSize) return;
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg, 3.5f, target));
            }
        }

        
        
        
        public static void Pump(ChatController chat)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            ProcessPendingWelcome();
            ProcessPendingGG();

            if (_queue.Count == 0) return;
            
            
            bool inLobby = ShipStatus.Instance == null;
            if (inLobby)
            {
                if (_lobbyReadyTime < 0f) return;
                if (Time.time < _lobbyReadyTime + LobbySettleSec) return;
            }

            
            var head = _queue.Peek();
            float minWait = head.wait >= 0f ? head.wait : ((ShipStatus.Instance == null) ? 1.0f : 0.8f);
            if (chat.timeSinceLastMessage < minWait) return;

            var (plain, colored, _, target) = _queue.Dequeue();
            
            if (target != null)
            {
                SendSystemMessage(target, plain, colored);
            }
            else
            {
                var speaker = LowestAlive() ?? PlayerControl.LocalPlayer;
                if (speaker == null) return;
                Send(speaker, plain, colored);
            }
            
            chat.timeSinceLastMessage = 0f;
        }
        
        private static void ProcessPendingGG()
        {
            if (!AmongUsClient.Instance.AmHost || _ggPlayerQueue.Count == 0) return;

            
            
            if (ShipStatus.Instance != null) return;
            
            
            
            if (_lobbyReadyTime < 0f) return;
            if (Time.time < _lobbyReadyTime + LobbySettleSec) return;

            float minWait = 3.5f;
            
            
            if (Time.time < _nextGgTime) return;
            
            
            if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.timeSinceLastMessage < minWait)
            {
                return;
            }
            
            
            byte pid = _ggPlayerQueue[0];
            _ggPlayerQueue.RemoveAt(0);
            
            PlayerControl target = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                if (pc?.PlayerId == pid) { target = pc; break; }
                
            if (target?.Data != null && target.OwnerId >= 0)
            {
                SendPrivate(target, GenerateGGMessagePlain(), GenerateGGMessageColored());
                Plugin.Log?.LogInfo($"[ChatManager] GG sent to {target.Data.PlayerName}!");
                
                
                if (HudManager.Instance?.Chat != null)
                    HudManager.Instance.Chat.timeSinceLastMessage = 0f;
                
                _nextGgTime = Time.time + 3.5f;
            }
        }

        private static void Send(PlayerControl speaker, string plainMsg, string coloredMsg)
        {
            
            try
            {
                IsSending = true;
                HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                IsSending = false;
            }
            catch { IsSending = false; }

            
            
            const string sysName = "Director";
            string orig = speaker.Data.PlayerName;

            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(5);
            w.Write(AmongUsClient.Instance.GameId);
            WSetName(w, speaker, sysName);
            WSendChat(w, speaker, SafeChat(plainMsg));
            WSetName(w, speaker, orig);
            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
            w.Recycle();
        }

        
        public static void SendSystemMessage(PlayerControl target, string plainMsg, string coloredMsg)
        {
            if (target == null || target.OwnerId < 0) return;
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null) return;

            try
            {
                const string sysName = "Director";
                string origSpeakerName = speaker.Data.PlayerName;

                if (target == PlayerControl.LocalPlayer)
                {
                    _colorMap[plainMsg] = coloredMsg;
                    IsSending = true;
                    HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                    IsSending = false;
                }
                else
                {
                    _colorMap[plainMsg] = coloredMsg;
                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WSetName(w, speaker, sysName);
                    WSendChat(w, speaker, SafeChat(plainMsg));
                    WSetName(w, speaker, origSpeakerName);
                    w.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(w);
                    w.Recycle();
                }

                Plugin.Log?.LogInfo($"[ChatManager] System message to {target.Data.PlayerName}: {plainMsg}");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogError($"[ChatManager/SystemMessage] Error sending to {target.Data.PlayerName}: {e.Message}");
            }
        }

        public static void SendPrivateTargeted(PlayerControl speaker, PlayerControl target, string plainMsg, string coloredMsg)
        {
            if (target == null || target.OwnerId < 0) return;
            if (speaker == null) return;
            bool inLobby = ShipStatus.Instance == null;

            
            _colorMap[plainMsg] = coloredMsg;

            // Afficher localement uniquement si le message est pour nous
            if (target == PlayerControl.LocalPlayer)
            {
                try
                {
                    IsSending = true;
                    HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                    IsSending = false;
                }
                catch { IsSending = false; }
            }
            else if (!inLobby || target != PlayerControl.LocalPlayer)
            {
                // Envoyer le message via réseau au destinataire (sauf si c'est pour nous en lobby)
                try
                {
                    const string sysName = "Director";
                    string orig = speaker.Data.PlayerName;

                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WSetName(w, speaker, sysName);
                    WSendChat(w, speaker, SafeChat(plainMsg));
                    WSetName(w, speaker, orig);
                    w.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(w);
                    w.Recycle();
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogError($"[ChatManager/PrivateTargeted] Error sending to {target.Data.PlayerName}: {e.Message}");
                }
            }

            Plugin.Log?.LogInfo($"[ChatManager] Private targeted message to {target.Data.PlayerName}: {plainMsg}");
        }

        private static PlayerControl LowestAlive()
        {
            PlayerControl best = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                if (best == null || pc.PlayerId < best.PlayerId) best = pc;
            }
            return best;
        }

        
        
        
        private static readonly List<(byte playerId, float earliestSendTime)> _welcomeQueue = new();
        private static float _nextWelcomeTime = 0f;
        private static readonly HashSet<byte> _sentWelcome = new();

        
        private const float WelcomeDelaySec = 3f;
        
        
        
        
        
        private const float LobbySettleSec = 7f;
        private static float _lobbyReadyTime = -1f;

        public static void ClearWelcomeSent()
        {
            _welcomeQueue.Clear();
            _nextWelcomeTime = 0f;
            _sentWelcome.Clear();
            _ggPlayerQueue.Clear();
            _nextGgTime = 0f;
            _lobbyReadyTime = -1f;
            _colorMap.Clear();
            _queue.Clear();
            Plugin.Log?.LogInfo("[ChatManager] Welcome, GG, and chat queue system cleared!");
        }
        
        public static void OnPlayerLeave(byte playerId)
        {
            if (_sentWelcome.Contains(playerId))
            {
                _sentWelcome.Remove(playerId);
                Plugin.Log?.LogInfo($"[ChatManager] Removed player {playerId} from sent welcome");
            }
            
            for (int i = _welcomeQueue.Count - 1; i >= 0; i--)
            {
                if (_welcomeQueue[i].playerId == playerId)
                {
                    _welcomeQueue.RemoveAt(i);
                    Plugin.Log?.LogInfo($"[ChatManager] Removed player {playerId} from welcome queue");
                    break;
                }
            }
        }

        public static void CheckNewPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ShipStatus.Instance != null) return;
            Plugin.Log?.LogInfo($"[ChatManager] CheckNewPlayers() called!");

            
            
            if (_lobbyReadyTime < 0f)
            {
                _lobbyReadyTime = Time.time;
                Plugin.Log?.LogInfo("[ChatManager] Lobby actif — départ du délai de stabilisation.");
            }
            
            
            var currentPlayerIds = new HashSet<byte>();
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                currentPlayerIds.Add(pc.PlayerId);
            }
            
            
            foreach (var id in _sentWelcome.ToArray())
            {
                if (!currentPlayerIds.Contains(id))
                {
                    _sentWelcome.Remove(id);
                    Plugin.Log?.LogInfo($"[ChatManager] Removed player {id} from sent welcome (no longer present)");
                }
            }
            
            for (int i = _welcomeQueue.Count - 1; i >= 0; i--)
            {
                if (!currentPlayerIds.Contains(_welcomeQueue[i].playerId))
                {
                    _welcomeQueue.RemoveAt(i);
                    Plugin.Log?.LogInfo($"[ChatManager] Removed player {_welcomeQueue[i].playerId} from welcome queue (no longer present)");
                }
            }
            
            
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                
                
                bool alreadyProcessed = _sentWelcome.Contains(pc.PlayerId);
                foreach (var item in _welcomeQueue)
                {
                    if (item.playerId == pc.PlayerId)
                    {
                        alreadyProcessed = true;
                        break;
                    }
                }
                if (alreadyProcessed) continue;

                
                float earliestSendTime = Mathf.Max(Time.time + WelcomeDelaySec, _lobbyReadyTime + LobbySettleSec);
                _welcomeQueue.Add((pc.PlayerId, earliestSendTime));
                Plugin.Log?.LogInfo($"[ChatManager] {pc.Data.PlayerName} (id={pc.PlayerId}) ajouté à la file d'attente, envoi possible à {earliestSendTime}!");
            }
        }

        private static void ProcessPendingWelcome()
        {
            if (!AmongUsClient.Instance.AmHost || _welcomeQueue.Count == 0) return;

            
            
            if (ShipStatus.Instance != null) return;
            
            
            if (_lobbyReadyTime < 0f) return;
            if (Time.time < _lobbyReadyTime + LobbySettleSec) return;

            float minWait = 3.5f;
            
            
            var firstItem = _welcomeQueue[0];
            byte pid = firstItem.playerId;
            float earliestSendTime = firstItem.earliestSendTime;
            
            
            if (Time.time < earliestSendTime) return;
            
            
            if (Time.time < _nextWelcomeTime) return;
            
            
            if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.timeSinceLastMessage < minWait)
            {
                return;
            }
            
            
            _welcomeQueue.RemoveAt(0);
            
            PlayerControl target = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                if (pc?.PlayerId == pid) { target = pc; break; }
                    
            if (target?.Data != null && target.OwnerId >= 0)
            {
                
                bool isReturningPlayer = DirectorCore.LastAlive.Contains(target.Data.PlayerName) || DirectorCore.LastDead.Contains(target.Data.PlayerName);
                bool hasGG = DirectorCore.LastAlive.Count > 0 || DirectorCore.LastDead.Count > 0;

                Plugin.Log?.LogInfo($"[ChatManager] Traitement de {target.Data.PlayerName} (id={pid}): isReturning={isReturningPlayer}, hasGG={hasGG}");
                
                if (isReturningPlayer && hasGG)
                {
                    
                    SendPrivate(target, GenerateGGMessagePlain(), GenerateGGMessageColored());
                    Plugin.Log?.LogInfo($"[ChatManager] GG envoyé à {target.Data.PlayerName} (joueur de retour)!");
                }
                else
                {
                    
                    SendPrivate(target, ModMessages.WelcomePlain, ModMessages.Welcome);
                    Plugin.Log?.LogInfo($"[ChatManager] Welcome envoyé à {target.Data.PlayerName} (nouveau joueur)!");
                }

                
                if (HudManager.Instance?.Chat != null)
                    HudManager.Instance.Chat.timeSinceLastMessage = 0f;
                
                _nextWelcomeTime = Time.time + 3.5f;
                _sentWelcome.Add(pid);
            }
        }



        private static void WSetName(MessageWriter w, PlayerControl p, string n)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetName); w.Write(p.Data.NetId); w.Write(n); w.EndMessage(); }
        private static void WSendChat(MessageWriter w, PlayerControl p, string m)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SendChat); w.Write(m); w.EndMessage(); }
        
        
        private static readonly List<byte> _ggPlayerQueue = new();
        private static float _nextGgTime = 0f;
        
        public static void SendPrivateGGToAll()
        {
            
            
            
            
            _ggPlayerQueue.Clear();
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data != null && !pc.Data.Disconnected && pc.OwnerId >= 0)
                    _ggPlayerQueue.Add(pc.PlayerId);
            }
            _nextGgTime = Time.time + 0.5f;
            Plugin.Log?.LogInfo($"[ChatManager] /gg manuel : {_ggPlayerQueue.Count} joueur(s) en file (envoi privé).");
        }

        public static string GenerateGGMessageColored()
        {
            var alive = DirectorCore.LastAlive;
            var dead = DirectorCore.LastDead;
            var director = DirectorCore.DirectorName ?? "aucun";
            Plugin.Log?.LogInfo($"[ChatManager] GenerateGGMessageColored - Alive count: {alive.Count}, Dead count: {dead.Count}, Director: {director}");
            if (alive.Count == 0 && dead.Count == 0)
                return ModMessages.GgNoGame;

            string TruncateList(List<string> list, string defaultText)
            {
                if (list.Count == 0) return defaultText;
                string result = string.Join(", ", list);
                while (result.Length > 40 && list.Count > 1)
                {
                    list.RemoveAt(list.Count - 1);
                    result = string.Join(", ", list) + " ...";
                }
                return result;
            }

            string s = TruncateList(new List<string>(alive), "aucun");
            string d = TruncateList(new List<string>(dead), "aucun");
            string msgPlain = string.Format(ModMessages.GgFormatPlain, s, d, director);
            
            if (msgPlain.Length > 120)
            {
                return ModMessages.GgSimple;
            }
            
            return string.Format(ModMessages.GgFormat, s, d, director);
        }
        
        public static string GenerateGGMessagePlain()
        {
            var alive = DirectorCore.LastAlive;
            var dead = DirectorCore.LastDead;
            var director = DirectorCore.DirectorName ?? "aucun";
            Plugin.Log?.LogInfo($"[ChatManager] GenerateGGMessagePlain - Alive count: {alive.Count}, Dead count: {dead.Count}, Director: {director}");
            if (alive.Count == 0 && dead.Count == 0)
                return ModMessages.GgNoGamePlain;

            string TruncateList(List<string> list, string defaultText)
            {
                if (list.Count == 0) return defaultText;
                string result = string.Join(", ", list);
                while (result.Length > 40 && list.Count > 1)
                {
                    list.RemoveAt(list.Count - 1);
                    result = string.Join(", ", list) + " ...";
                }
                return result;
            }

            string s = TruncateList(new List<string>(alive), "aucun");
            string d = TruncateList(new List<string>(dead), "aucun");
            string msg = string.Format(ModMessages.GgFormatPlain, s, d, director);
            
            
            if (msg.Length > 120)
            {
                return ModMessages.GgSimplePlain;
            }
            
            return msg;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    static class ChatPump_P
    { static void Postfix(ChatController __instance) => ChatManager.Pump(__instance); }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    static class ChatAddColor_P
    {
        static void Prefix(PlayerControl sourcePlayer, ref string chatText)
        {
            
            if (!string.IsNullOrWhiteSpace(chatText))
            {
                string coloredText = null;
                if (ChatManager._colorMap.TryGetValue(chatText, out coloredText))
                {
                    chatText = coloredText;
                    Plugin.Log?.LogInfo($"[ChatColor] Replaced plain text with colored: {coloredText}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    static class PlayerJoined_Patch
    {
        static void Postfix(PlayerControl __instance)
        {
            Plugin.Log?.LogInfo($"[ChatManager] PlayerControl.Start called for {__instance?.Data?.PlayerName} (id={__instance?.PlayerId})");
            ChatManager.CheckNewPlayers();
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    static class HudStartPatch
    {
        static void Postfix()
        {
            Plugin.Log?.LogInfo("[ChatManager] HudManager started - calling CheckNewPlayers!");
            ChatManager.CheckNewPlayers();
        }
    }
}
