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
        // Chaque message porte son propre délai minimum. wait < 0 => délai par défaut (cadence normale).
        private static readonly Queue<(string plain, string colored, float wait)> _queue = new();
        private const int MaxQueueSize = 20; // Prevent queue from getting too large

        // Limite DURE d'Among Us vanilla : 100 caractères par message (texte réseau, sans balises <color>).
        private const int MaxChatChars = 100;
        private const int MaxChatBytes = 100;
        public static string SafeChat(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length > MaxChatChars) s = s.Substring(0, MaxChatChars - 3) + "...";
            // Les caractères accentués (é, É, …) comptent pour plusieurs octets en
            // UTF-8 : on tronque aussi sur la taille réseau pour rester sous la
            // limite serveur et éviter un rejet/déconnexion de l'hôte.
            while (System.Text.Encoding.UTF8.GetByteCount(s) > MaxChatBytes && s.Length > 1)
                s = s.Substring(0, s.Length - 1);
            return s;
        }

        // --- MÉTHODE ORIGINALE POUR /WELCOME ET /GG --- (NE PAS TOUCHER)
        public static void SendPrivate(PlayerControl target, string plainMessage)
        {
            if (target == null || target.OwnerId < 0) return;
            
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null) return;
            
            try
            {
                // First show it on host's chat locally
                try
                {
                    IsSending = true;
                    HudManager.Instance.Chat.AddChat(speaker, plainMessage);
                    IsSending = false;
                }
                catch { IsSending = false; }
                
                // ONLY send network message if we're in a meeting!
                if (MeetingHud.Instance != null)
                {
                    Plugin.Log?.LogInfo($"[ChatManager] Envoi du message en meeting à {target.Data.PlayerName}");
                    
                    // In meetings, normal SendChat RPC is safe and won't kick the host!
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        speaker.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, target.OwnerId);
                    writer.Write(SafeChat(plainMessage));
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
                
                Plugin.Log?.LogInfo($"[ChatManager] Message traité pour {target.Data.PlayerName}: {plainMessage}");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogError($"[ChatManager] Erreur envoi message: {e.Message}");
            }
        }

        // --- MÉTHODE SUPPLÉMENTAIRE POUR LES ORDRES DE SCRIPT ---
        public static void SendPrivateScript(PlayerControl target, string plainMessage, string coloredMessage)
        {
            if (target == null || target.OwnerId < 0) return;
            
            var speaker = PlayerControl.LocalPlayer;
            if (speaker == null) return;
            
            try
            {
                // Enregistre la correspondance plain → colored
                _colorMap[plainMessage] = coloredMessage;
                
                // First show it on host's chat locally
                try
                {
                    IsSending = true;
                    HudManager.Instance.Chat.AddChat(speaker, coloredMessage);
                    IsSending = false;
                }
                catch { IsSending = false; }
                
                // ONLY send network message if we're in a meeting or lobby! (to avoid host kick)
                if (MeetingHud.Instance != null || ShipStatus.Instance == null)
                {
                    Plugin.Log?.LogInfo($"[ChatManager] Envoi du message de script en lobby/meeting à {target.Data.PlayerName}");
                    
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        speaker.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, target.OwnerId);
                    writer.Write(SafeChat(plainMessage));
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
                
                Plugin.Log?.LogInfo($"[ChatManager] Message de script traité pour {target.Data.PlayerName}: {plainMessage}");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogError($"[ChatManager] Erreur envoi message de script: {e.Message}");
            }
        }

        // --- MÉTHODE PRIVÉE D'ORIGINE POUR ProcessPendingWelcome et ProcessPendingGG --- (NE PAS TOUCHER)
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
                writer.Write(SafeChat(plainMsg));
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                
                Plugin.Log?.LogInfo($"[ChatManager] Message envoyé (privé) → {target.Data.PlayerName}: {plainMsg}");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[SendPrivate] {e.Message}"); }
        }

        public static void Queue(string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg))
            {
                if (_queue.Count >= MaxQueueSize) return; // Prevent queue overflow
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg, -1f)); // -1 = cadence par défaut
            }
        }

        // Overload pour garder la compatibilité
        public static void Queue(string coloredMsg)
        {
            string plainMsg = System.Text.RegularExpressions.Regex.Replace(coloredMsg, "<[^>]*>", "");
            Queue(coloredMsg, plainMsg);
        }

        // Réservé à /players, /help, /welcome et /gg : impose 3.5s avant CHAQUE message de la file.
        public static void QueueSlow(string coloredMsg, string plainMsg)
        {
            if (!string.IsNullOrWhiteSpace(coloredMsg) && !string.IsNullOrWhiteSpace(plainMsg))
            {
                if (_queue.Count >= MaxQueueSize) return; // Prevent queue overflow
                _colorMap[plainMsg] = coloredMsg;
                _queue.Enqueue((plainMsg, coloredMsg, 3.5f));
            }
        }

        // ────────────────────────────────────────────────
        // Pompe (timer natif du jeu)
        // ────────────────────────────────────────────────
        public static void Pump(ChatController chat)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            ProcessPendingWelcome();
            ProcessPendingGG();

            if (_queue.Count == 0) return;
            
            // Attendre que le lobby soit stable avant d'envoyer des messages publics
            bool inLobby = ShipStatus.Instance == null;
            if (inLobby)
            {
                if (_lobbyReadyTime < 0f) return;
                if (Time.time < _lobbyReadyTime + LobbySettleSec) return;
            }

            // Délai propre au message : cadence normale par défaut, 3.5s pour /players et /help.
            var head = _queue.Peek();
            float minWait = head.wait >= 0f ? head.wait : ((ShipStatus.Instance == null) ? 1.0f : 0.8f);
            if (chat.timeSinceLastMessage < minWait) return;

            var speaker = LowestAlive() ?? PlayerControl.LocalPlayer;
            if (speaker == null) return;

            var (plain, colored, _) = _queue.Dequeue();
            Send(speaker, plain, colored);
            chat.timeSinceLastMessage = 0f;
        }
        
        private static void ProcessPendingGG()
        {
            if (!AmongUsClient.Instance.AmHost || _ggPlayerQueue.Count == 0) return;

            // Sécurité : uniquement en lobby. Envoyer un chat en pleine partie /
            // pendant l'écran de fin est illégal côté serveur et kicke l'hôte.
            if (ShipStatus.Instance != null) return;
            
            // Attendre que le lobby soit stable (même principe que pour le welcome)
            // pour éviter de kicker l'hôte quand des joueurs vanilla arrivent en même temps.
            if (_lobbyReadyTime < 0f) return;
            if (Time.time < _lobbyReadyTime + LobbySettleSec) return;

            float minWait = 3.5f;
            
            // Check if it's time to send next GG
            if (Time.time < _nextGgTime) return;
            
            // Check if enough time has passed since last chat message (3.5s)
            if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.timeSinceLastMessage < minWait)
            {
                return;
            }
            
            // Take first player in queue
            byte pid = _ggPlayerQueue[0];
            _ggPlayerQueue.RemoveAt(0);
            
            PlayerControl target = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                if (pc?.PlayerId == pid) { target = pc; break; }
                
            if (target?.Data != null && target.OwnerId >= 0)
            {
                SendPrivate(target, GenerateGGMessagePlain(), GenerateGGMessageColored());
                Plugin.Log?.LogInfo($"[ChatManager] GG sent to {target.Data.PlayerName}!");
                
                // Reset chat time and schedule next GG
                if (HudManager.Instance?.Chat != null)
                    HudManager.Instance.Chat.timeSinceLastMessage = 0f;
                
                _nextGgTime = Time.time + 3.5f;
            }
        }

        private static void Send(PlayerControl speaker, string plainMsg, string coloredMsg)
        {
            bool inLobby = ShipStatus.Instance == null;

            // First show it on host's chat locally
            try
            {
                IsSending = true;
                HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
                IsSending = false;
            }
            catch { IsSending = false; }

            if (inLobby)
            {
                // In lobby, send targeted SendChat RPC to each player (works fine)
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (pc?.Data == null || pc.OwnerId < 0 || pc == PlayerControl.LocalPlayer) continue;

                    try
                    {
                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            speaker.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, pc.OwnerId);
                        writer.Write(SafeChat(plainMsg));
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log?.LogError($"[ChatManager/Lobby] Error sending to player {pc.Data.PlayerName}: {e.Message}");
                    }
                }
            }
            else
            {
                // IN GAME: use GameData message (StartMessage(5)) with SetName, SendChat, SetName, like original code!
                // This is what vanilla clients expect during a game!
                const string sysName = "The Director's Cut";
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
        // Welcome privé automatique
        // ────────────────────────────────────────────────
        private static readonly List<(byte playerId, float earliestSendTime)> _welcomeQueue = new();
        private static float _nextWelcomeTime = 0f;
        private static readonly HashSet<byte> _sentWelcome = new();

        // Délai par joueur depuis sa détection (couvre l'arrivée tardive).
        private const float WelcomeDelaySec = 3f;
        // Délai de stabilisation depuis le (re)chargement du lobby. Quand l'hôte
        // ET des clients arrivent EN MÊME TEMPS (retour de partie groupé), les
        // clients vanilla n'ont pas encore fini de se synchroniser à +3s : leur
        // envoyer un chat les fait diverger et l'hôte se fait kick. On attend
        // donc que le lobby soit stable depuis assez longtemps.
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
            Plugin.Log?.LogInfo("[ChatManager] Welcome and GG system cleared!");
        }
        
        public static void OnPlayerLeave(byte playerId)
        {
            if (_sentWelcome.Contains(playerId))
            {
                _sentWelcome.Remove(playerId);
                Plugin.Log?.LogInfo($"[ChatManager] Removed player {playerId} from sent welcome");
            }
            // Remove from welcome queue
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

            // Premier passage dans CE lobby : on note l'instant où il est devenu
            // actif (point de départ du délai de stabilisation).
            if (_lobbyReadyTime < 0f)
            {
                _lobbyReadyTime = Time.time;
                Plugin.Log?.LogInfo("[ChatManager] Lobby actif — départ du délai de stabilisation.");
            }
            
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
            // Remove from welcome queue
            for (int i = _welcomeQueue.Count - 1; i >= 0; i--)
            {
                if (!currentPlayerIds.Contains(_welcomeQueue[i].playerId))
                {
                    _welcomeQueue.RemoveAt(i);
                    Plugin.Log?.LogInfo($"[ChatManager] Removed player {_welcomeQueue[i].playerId} from welcome queue (no longer present)");
                }
            }
            
            // Now check for new players
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                
                // Check if player is already in sent or queue
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

                // Calculate earliest send time
                float earliestSendTime = Mathf.Max(Time.time + WelcomeDelaySec, _lobbyReadyTime + LobbySettleSec);
                _welcomeQueue.Add((pc.PlayerId, earliestSendTime));
                Plugin.Log?.LogInfo($"[ChatManager] {pc.Data.PlayerName} (id={pc.PlayerId}) ajouté à la file d'attente, envoi possible à {earliestSendTime}!");
            }
        }

        private static void ProcessPendingWelcome()
        {
            if (!AmongUsClient.Instance.AmHost || _welcomeQueue.Count == 0) return;

            // Sécurité : uniquement en lobby. Envoyer un chat en pleine partie /
            // pendant l'écran de fin est illégal côté serveur et kicke l'hôte.
            if (ShipStatus.Instance != null) return;
            
            // Attendre que le lobby soit stable
            if (_lobbyReadyTime < 0f) return;
            if (Time.time < _lobbyReadyTime + LobbySettleSec) return;

            float minWait = 3.5f;
            
            // Get first player in queue
            var firstItem = _welcomeQueue[0];
            byte pid = firstItem.playerId;
            float earliestSendTime = firstItem.earliestSendTime;
            
            // Check if we can send this player's message yet
            if (Time.time < earliestSendTime) return;
            
            // Check if it's time to send next welcome/gg (global delay)
            if (Time.time < _nextWelcomeTime) return;
            
            // Check if enough time has passed since last chat message
            if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.timeSinceLastMessage < minWait)
            {
                return;
            }
            
            // Remove from queue now
            _welcomeQueue.RemoveAt(0);
            
            PlayerControl target = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                if (pc?.PlayerId == pid) { target = pc; break; }
                    
            if (target?.Data != null && target.OwnerId >= 0)
            {
                // Vérifier si le joueur était présent dans la partie précédente
                bool isReturningPlayer = DirectorCore.LastAlive.Contains(target.Data.PlayerName) || DirectorCore.LastDead.Contains(target.Data.PlayerName);
                bool hasGG = DirectorCore.LastAlive.Count > 0 || DirectorCore.LastDead.Count > 0;

                Plugin.Log?.LogInfo($"[ChatManager] Traitement de {target.Data.PlayerName} (id={pid}): isReturning={isReturningPlayer}, hasGG={hasGG}");
                
                if (isReturningPlayer && hasGG)
                {
                    // Joueur de retour → envoie /gg
                    SendPrivate(target, GenerateGGMessagePlain(), GenerateGGMessageColored());
                    Plugin.Log?.LogInfo($"[ChatManager] GG envoyé à {target.Data.PlayerName} (joueur de retour)!");
                }
                else
                {
                    // Nouveau joueur → envoie seulement /welcome
                    SendPrivate(target, ModMessages.WelcomePlain, ModMessages.Welcome);
                    Plugin.Log?.LogInfo($"[ChatManager] Welcome envoyé à {target.Data.PlayerName} (nouveau joueur)!");
                }

                // Reset chat time and schedule next welcome/gg
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
        
        // Pour envoyer des GG en privé à tous les joueurs
        private static readonly List<byte> _ggPlayerQueue = new();
        private static float _nextGgTime = 0f;
        
        public static void SendPrivateGGToAll()
        {
            // /gg MANUEL : envoi privé joueur par joueur (même canal que le
            // welcome), traité par ProcessPendingGG, un message toutes les 3.5s,
            // et seulement en lobby. L'auto-GG de fin de partie, lui, passe par
            // le flux welcome (étape 1).
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
