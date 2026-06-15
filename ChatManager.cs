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

        // ===== Identité du bot =====
        // Pseudo affiché par le bot dans le chat. Les balises <color> sont rendues
        // dans le nom (pas d'anti-cheat sur serveur privé). Bleu.
        private const string BotName = "<color=#3B9DFF>The Director's Cut</color>";
        // Couleur d'avatar du bot dans la bulle de chat. 1 = Blue (cf. Palette.ColorNames).
        private const byte BotColorId = 1;
        // Si true, on change aussi la couleur de l'avatar (en plus du nom).
        // Piloté par la config (BepInEx) ; true par défaut si la config n'est pas prête.
        private static bool BotCosmetics => ModConfig.BotCosmetics?.Value ?? true;

        
        internal static readonly Dictionary<string, string> _colorMap = new();

        
        
        
        private static readonly Queue<(string plain, string colored, float wait, PlayerControl? target)> _queue = new();
        private const int MaxQueueSize = 20; 

        
        // Plus de découpage : on autorise les longs messages formatés (gras, couleurs,
        // sauts de ligne). On garde juste un plafond de sécurité large pour ne pas
        // dépasser la taille d'un paquet Hazel fiable (~MTU) et risquer un drop.
        private const int MaxChatChars = 1500;
        private const int MaxChatBytes = 1200;
        public static string SafeChat(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length > MaxChatChars) s = s.Substring(0, MaxChatChars - 3) + "...";

            // Filet de sécurité transport : ne tronque que les messages réellement énormes.
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
                    AddChatLocal(speaker, coloredMsg);
                }
                else
                {
                    // Envoyer le message via réseau au destinataire (privé → tag 6 ciblé)
                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WBotChat(w, speaker, coloredMsg);
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
                    AddChatLocal(speaker, coloredMsg);
                }

                _colorMap[plainMsg] = coloredMsg;

                if (inLobby || inMeeting)
                {

                    SendPrivate(target, plainMsg, coloredMsg);
                }
                else
                {
                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WBotChat(w, speaker, coloredMsg);
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
            _colorMap[plainMsg] = coloredMsg;
            AddChatLocal(speaker, coloredMsg);
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

            // Plus d'attente artificielle : on vide TOUTE la file dès cette frame.
            // Les réponses aux commandes s'affichent donc instantanément, tout en
            // restant dans le contexte sûr de ChatController.Update et en conservant
            // la confidentialité (messages ciblés via SendSystemMessage / tag 6).
            while (_queue.Count > 0)
            {
                var (plain, colored, _, target) = _queue.Dequeue();

                if (target != null)
                {
                    SendSystemMessage(target, plain, colored);
                }
                else
                {
                    var speaker = LowestAlive() ?? PlayerControl.LocalPlayer;
                    if (speaker == null) break;
                    Send(speaker, plain, colored);
                }
            }

            chat.timeSinceLastMessage = 0f;
        }
        
        private static void ProcessPendingGG()
        {
            if (!AmongUsClient.Instance.AmHost || _ggPlayerQueue.Count == 0) return;
            if (ShipStatus.Instance != null) return; // GG en lobby uniquement

            // Instantané : on envoie le GG à TOUS les joueurs en file, d'un coup.
            while (_ggPlayerQueue.Count > 0)
            {
                byte pid = _ggPlayerQueue[0];
                _ggPlayerQueue.RemoveAt(0);

                PlayerControl target = null;
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    if (pc?.PlayerId == pid) { target = pc; break; }

                if (target?.Data != null && target.OwnerId >= 0)
                {
                    SendPrivate(target, GenerateGGMessagePlain(), GenerateGGMessageColored());
                    Plugin.Log?.LogInfo($"[ChatManager] GG envoyé à {target.Data.PlayerName} !");
                }
            }
        }

        private static void Send(PlayerControl speaker, string plainMsg, string coloredMsg)
        {
            // Affichage local (hôte) sous l'identité du bot
            AddChatLocal(speaker, coloredMsg);

            // Diffusion à tous (tag 5) sous l'identité du bot
            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(5);
            w.Write(AmongUsClient.Instance.GameId);
            WBotChat(w, speaker, plainMsg);
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
                if (target == PlayerControl.LocalPlayer)
                {
                    _colorMap[plainMsg] = coloredMsg;
                    AddChatLocal(speaker, coloredMsg);
                }
                else
                {
                    // Si l'hôte est mort, on emprunte l'identité d'un joueur vivant pour
                    // envoyer le chat réseau : sinon le serveur kick l'hôte pour avoir
                    // fait parler un personnage mort.
                    var netSpeaker = (speaker.Data?.IsDead == true) ? (LowestAlive() ?? speaker) : speaker;

                    _colorMap[plainMsg] = coloredMsg;
                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WBotChat(w, netSpeaker, coloredMsg);
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
                AddChatLocal(speaker, coloredMsg);
            }
            else if (!inLobby || target != PlayerControl.LocalPlayer)
            {
                // Envoyer le message via réseau au destinataire (sauf si c'est pour nous en lobby)
                try
                {
                    var w = MessageWriter.Get(SendOption.Reliable);
                    w.StartMessage(6);
                    w.Write(AmongUsClient.Instance.GameId);
                    w.WritePacked(target.OwnerId);
                    WBotChat(w, speaker, coloredMsg);
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

        
        // Délais neutralisés : welcome/GG instantanés (pas d'attente ni d'anti-spam).
        private const float WelcomeDelaySec = 0f;
        private const int MaxWelcomesPerWave = 99999;
        private const float WaveResetWindow = 10f;

        private static float LobbySettleSec = 0f;
        private static float _lobbyReadyTime = -1f;
        private static int _welcomesSentInWave = 0;
        private static float _waveResetTime = 0f;
        private static bool _fallbackMode = false;

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
            _welcomesSentInWave = 0;
            _waveResetTime = 0f;
            _fallbackMode = false;
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

        // Conservé pour les patches Start/HudStart, mais le welcome est désormais géré en
        // continu par ProcessPendingWelcome (appelé chaque frame depuis Pump). No-op.
        public static void CheckNewPlayers() { }

        // Construit le message de bienvenue, en y ajoutant le lien Discord configuré (si défini).
        private static (string plain, string colored) BuildWelcome()
        {
            string colored = ModMessages.Welcome;
            string plain = ModMessages.WelcomePlain;
            string link = ModConfig.DiscordLink?.Value;
            if (!string.IsNullOrWhiteSpace(link))
            {
                colored += $"\n<b><color=#5865F2>Discord</color></b> : <color=#5865F2><u>{link}</u></color>";
                plain += $"\nDiscord : {link}";
            }
            // Pseudos pour les ajouts directs (alternative à la copie du lien)
            colored += "\n" + ModMessages.DiscordContacts;
            plain += "\n" + ModMessages.DiscordContactsPlain;
            return (plain, colored);
        }

        // Oublie qui a déjà été accueilli (appelé à la fin d'une partie) pour que tous les
        // joueurs de retour au lobby reçoivent le récap GG.
        public static void ClearSentWelcome()
        {
            _sentWelcome.Clear();
            Plugin.Log?.LogInfo("[ChatManager] Suivi welcome réinitialisé (fin de partie → récap GG).");
        }

        // REFONTE : plus de file ni de hooks fragiles. À chaque frame (en lobby), on scanne
        // tous les joueurs présents et on envoie le message à tout joueur pas encore accueilli.
        // Auto-réparant : si un joueur n'était pas prêt (nom/OwnerId pas encore reçus), il sera
        // accueilli dès la frame suivante. Joueur "de retour" → récap GG, sinon → welcome.
        private static void ProcessPendingWelcome()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ShipStatus.Instance != null) return;            // lobby uniquement
            if (HudManager.Instance?.Chat == null) return;      // chat prêt

            var currentIds = new HashSet<byte>();
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc?.Data == null) continue;
                currentIds.Add(pc.PlayerId);

                if (pc.OwnerId < 0 || pc.Data.Disconnected) continue;
                if (string.IsNullOrEmpty(pc.Data.PlayerName)) continue; // pas encore initialisé
                if (_sentWelcome.Contains(pc.PlayerId)) continue;

                bool isReturning = DirectorCore.LastAlive.Contains(pc.Data.PlayerName) || DirectorCore.LastDead.Contains(pc.Data.PlayerName);
                bool hasGG = DirectorCore.LastAlive.Count > 0 || DirectorCore.LastDead.Count > 0;

                if (isReturning && hasGG)
                {
                    SendPrivate(pc, GenerateGGMessagePlain(), GenerateGGMessageColored());
                    Plugin.Log?.LogInfo($"[ChatManager] GG envoyé à {pc.Data.PlayerName} (retour) !");
                }
                else
                {
                    var (wp, wc) = BuildWelcome();
                    SendPrivate(pc, wp, wc);
                    Plugin.Log?.LogInfo($"[ChatManager] Welcome envoyé à {pc.Data.PlayerName} !");
                }

                _sentWelcome.Add(pc.PlayerId);
            }

            // Oublier les joueurs partis pour les ré-accueillir s'ils reviennent.
            _sentWelcome.RemoveWhere(id => !currentIds.Contains(id));
        }



        private static void WSetName(MessageWriter w, PlayerControl p, string n)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetName); w.Write(p.Data.NetId); w.Write(n); w.EndMessage(); }
        private static void WSendChat(MessageWriter w, PlayerControl p, string m)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SendChat); w.Write(m); w.EndMessage(); }
        // SetColor RPC : payload = (uint32 Data.NetId)(byte colorId). Confirmé par l'anticheat.
        private static void WSetColor(MessageWriter w, PlayerControl p, byte color)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetColor); w.Write(p.Data.NetId); w.Write(color); w.EndMessage(); }

        // Écrit dans le writer la séquence "le bot parle" : on bascule cosmétiques + nom
        // sur le bot, on envoie le chat, puis on remet l'apparence d'origine du speaker.
        // Le nom et l'avatar sont figés par la bulle de chat au moment de l'AddChat distant,
        // donc le retour à la normale juste après n'affecte pas le message déjà affiché.
        // msg = texte RICH (gras/couleurs/\n). Envoyé tel quel à tous les clients pour
        // qu'ils voient le formatage (les clients vanilla rendent le rich text TMP).
        private static void WBotChat(MessageWriter w, PlayerControl p, string msg)
        {
            string origName = p.Data.PlayerName;
            byte origColor = (byte)p.Data.DefaultOutfit.ColorId;
            if (BotCosmetics) WSetColor(w, p, BotColorId);
            WSetName(w, p, BotName);
            WSendChat(w, p, SafeChat(msg));
            WSetName(w, p, origName);
            if (BotCosmetics) WSetColor(w, p, origColor);
        }

        // Affichage LOCAL (côté hôte) sous l'identité du bot : on force temporairement le
        // pseudo de l'hôte sur le bot le temps que la bulle se crée, puis on le remet.
        // Purement local (aucun RPC), sans risque réseau.
        private static void AddChatLocal(PlayerControl speaker, string coloredMsg)
        {
            if (speaker == null || HudManager.Instance?.Chat == null) return;
            string origName = speaker.Data.PlayerName;
            try
            {
                IsSending = true;
                speaker.Data.PlayerName = BotName;
                HudManager.Instance.Chat.AddChat(speaker, coloredMsg);
            }
            catch (Exception e) { Plugin.Log?.LogError($"[AddChatLocal] {e.Message}"); }
            finally
            {
                speaker.Data.PlayerName = origName;
                IsSending = false;
            }
        }
        
        
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
            _nextGgTime = Time.time + 0f;
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
