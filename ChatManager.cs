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

        // Dictionnaire pour mapper texte simple → texte coloré (seulement local !)
        internal static readonly Dictionary<string, string> _colorMap = new();

        // ────────────────────────────────────────────────
        // File d'attente
        // ────────────────────────────────────────────────
        private static readonly Queue<(string plain, string colored)> _queue = new();

        public static void Queue(string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg))
            {
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg));
            }
        }

        // Overload pour garder la compatibilité
        public static void Queue(string coloredMsg)
        {
            string plainMsg = System.Text.RegularExpressions.Regex.Replace(coloredMsg, "<[^>]*>", "");
            Queue(coloredMsg, plainMsg);
        }

        // ────────────────────────────────────────────────
        // Pompe (timer natif du jeu)
        // ────────────────────────────────────────────────
        public static void Pump(ChatController chat)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            ProcessPendingWelcome();

            if (_queue.Count == 0) return;

            float minWait = (ShipStatus.Instance == null) ? 1.0f : 0.8f;
            if (chat.timeSinceLastMessage < minWait) return;

            var speaker = LowestAlive() ?? PlayerControl.LocalPlayer;
            if (speaker == null) return;

            var (plain, colored) = _queue.Dequeue();
            Send(speaker, plain, colored);
            chat.timeSinceLastMessage = 0f;
        }

        private static void Send(PlayerControl speaker, string plainMsg, string coloredMsg)
        {
            bool inLobby = ShipStatus.Instance == null;

            if (inLobby)
            {
                Plugin.Log?.LogInfo($"[ChatManager/Lobby] Sending lobby message to all: {plainMsg}");
                
                // First show it on host's chat locally
                try
                {
                    IsSending = true;
                    HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                    IsSending = false;
                }
                catch { IsSending = false; }
                
                // Then send RPC to other players
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (pc?.Data == null || pc.OwnerId < 0 || pc == PlayerControl.LocalPlayer) continue;

                    try
                    {
                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, pc.OwnerId);
                        writer.Write(plainMsg);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log?.LogError($"[ChatManager/Lobby] Error sending to player {pc.Data.PlayerName}: {e.Message}");
                    }
                }
                return;
            }

            const string sysName = "[ The Director's Cut ]";
            string orig = speaker.Data.PlayerName;
            try
            {
                speaker.SetName(sysName);
                IsSending = true;
                HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                IsSending = false;
                speaker.SetName(orig);
            }
            catch { IsSending = false; speaker.SetName(orig); }

            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(5);
            w.Write(AmongUsClient.Instance.GameId);
            WSetName(w, speaker, sysName);
            WSendChat(w, speaker, plainMsg);
            WSetName(w, speaker, orig);
            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
            w.Recycle();
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

        // ────────────────────────────────────────────────
        // Welcome privé automatique (AVEC DELAI DE 3s !!)
        // ────────────────────────────────────────────────
        private static readonly Dictionary<byte, (float time, int step)> _pendingWelcome = new();
        private static readonly HashSet<byte> _sentWelcome = new();

        public static void ClearWelcomeSent()
        {
            _pendingWelcome.Clear();
            _sentWelcome.Clear();
            _colorMap.Clear();
            Plugin.Log?.LogInfo("[ChatManager] Welcome system cleared!");
        }
        
        public static void OnPlayerLeave(byte playerId)
        {
            if (_sentWelcome.Contains(playerId))
            {
                _sentWelcome.Remove(playerId);
                Plugin.Log?.LogInfo($"[ChatManager] Removed player {playerId} from sent welcome");
            }
            if (_pendingWelcome.ContainsKey(playerId))
            {
                _pendingWelcome.Remove(playerId);
                Plugin.Log?.LogInfo($"[ChatManager] Removed player {playerId} from pending welcome");
            }
        }

        public static void CheckNewPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ShipStatus.Instance != null) return;
            Plugin.Log?.LogInfo($"[ChatManager] CheckNewPlayers() called!");
            
            // First: collect current player IDs
            var currentPlayerIds = new HashSet<byte>();
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                currentPlayerIds.Add(pc.PlayerId);
            }
            
            // Remove players who are no longer present
            foreach (var id in _sentWelcome.ToArray())
            {
                if (!currentPlayerIds.Contains(id))
                {
                    _sentWelcome.Remove(id);
                    Plugin.Log?.LogInfo($"[ChatManager] Removed player {id} from sent welcome (no longer present)");
                }
            }
            foreach (var id in _pendingWelcome.Keys.ToArray())
            {
                if (!currentPlayerIds.Contains(id))
                {
                    _pendingWelcome.Remove(id);
                    Plugin.Log?.LogInfo($"[ChatManager] Removed player {id} from pending welcome (no longer present)");
                }
            }
            
            // Now check for new players
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                if (_sentWelcome.Contains(pc.PlayerId) || _pendingWelcome.ContainsKey(pc.PlayerId)) continue;
                _pendingWelcome[pc.PlayerId] = (Time.time + 3f, 0); // Attend 3 secondes
                Plugin.Log?.LogInfo($"[ChatManager] Welcome programmé pour {pc.Data.PlayerName} (id={pc.PlayerId})!");
            }
        }

        private static void ProcessPendingWelcome()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            
            // Skip welcome if we have a pending auto GG
            if (DirectorCore.PendingAutoGG) return;
            
            float minWait = (ShipStatus.Instance == null) ? 1.0f : 0.8f;
            
            foreach (var pid in _pendingWelcome.Keys.ToList())
            {
                var (time, step) = _pendingWelcome[pid];
                Plugin.Log?.LogInfo($"[ChatManager] Checking pending welcome for player {pid} (step {step}): delay elapsed? {Time.time >= time}");
                if (Time.time < time) continue;
                
                // Check if enough time has passed since last chat message
                if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.timeSinceLastMessage < minWait)
                {
                    Plugin.Log?.LogInfo($"[ChatManager] Waiting to send welcome to {pid} (last message too recent: {HudManager.Instance.Chat.timeSinceLastMessage:F2}s < {minWait}s)");
                    continue;
                }
                
                PlayerControl target = null;
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    if (pc?.PlayerId == pid) { target = pc; break; }
                    
                if (target?.Data != null && target.OwnerId >= 0)
                {
                    if (step == 0)
                    {
                        SendPrivate(target, ModMessages.WelcomePlain, ModMessages.Welcome);
                    }
                    
                    Plugin.Log?.LogInfo($"[ChatManager] Welcome step {step} sent to {target.Data.PlayerName}!");
                    
                    // Reset chat time since last message to respect rate limits
                    if (HudManager.Instance?.Chat != null)
                        HudManager.Instance.Chat.timeSinceLastMessage = 0f;
                    
                    int totalSteps = 1; // Seulement le message de bienvenue
                    if (step + 1 < totalSteps)
                    {
                        _pendingWelcome[pid] = (Time.time + 3f, step + 1); // Next message after 3 sec
                    }
                    else
                    {
                        _pendingWelcome.Remove(pid);
                        _sentWelcome.Add(pid);
                        Plugin.Log?.LogInfo($"[ChatManager] All welcome messages sent to {target.Data.PlayerName}!");
                    }
                }
                else
                {
                    _pendingWelcome.Remove(pid);
                    Plugin.Log?.LogInfo($"[ChatManager] Could not find target for pid {pid}!");
                }
            }
        }

        private static void SendPrivate(PlayerControl target, string plainMsg, string coloredMsg)
        {
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null || target == null) return;
            try
            {
                // Enregistre la correspondance plain → colored
                _colorMap[plainMsg] = coloredMsg;
                
                // Envoie la version plain text via RPC (pas de couleurs !)
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    speaker.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, target.OwnerId);
                writer.Write(plainMsg);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                
                Plugin.Log?.LogInfo($"[ChatManager] Message envoyé (privé) → {target.Data.PlayerName}: {plainMsg}");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[SendPrivate] {e.Message}"); }
        }

        private static void WSetName(MessageWriter w, PlayerControl p, string n)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetName); w.Write(p.Data.NetId); w.Write(n); w.EndMessage(); }
        private static void WSendChat(MessageWriter w, PlayerControl p, string m)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SendChat); w.Write(m); w.EndMessage(); }
        
        public static string GenerateGGMessageColored()
        {
            var alive = DirectorCore.LastAlive;
            var dead = DirectorCore.LastDead;
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
            string msgPlain = string.Format(ModMessages.GgFormatPlain, s, d);
            
            if (msgPlain.Length > 120)
            {
                return ModMessages.GgSimple;
            }
            
            return string.Format(ModMessages.GgFormat, s, d);
        }
        
        public static string GenerateGGMessagePlain()
        {
            var alive = DirectorCore.LastAlive;
            var dead = DirectorCore.LastDead;
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
            string msg = string.Format(ModMessages.GgFormatPlain, s, d);
            
            // Vérification finale : si c'est trop long, on simplifie encore
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
            // Si on a une version colorée pour ce texte, on remplace !
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
