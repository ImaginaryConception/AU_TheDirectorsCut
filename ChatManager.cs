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
        private static float? _pendingGGTime = null;

        // ── Messages ────────────────────────────────────────
        public static readonly string WelcomeMsg =
            "THE DIRECTOR'S CUT: 1er mort = RÉALISATEUR, Autres = ACTEURS. Tapez /help.";

        public static readonly string[] HelpMessages =
        {
            "<color=#88ccff>DIRECTIVES</color> : <color=#ff6b6b>/cut</color> (ne bougez) | <color=#ff6b6b>/swap</color> A B | <color=#ff6b6b>/blind</color> ID | <color=#ff6b6b>/darkness</color>",
            "Suite : <color=#ff6b6b>/freeze</color> ID | <color=#ff6b6b>/spin</color> ID | <color=#ff6b6b>/randomcolors</color> | <color=#ff6b6b>/shuffle</color> | <color=#ff6b6b>/teleportall</color> ID",
        };

        public static string GenerateGGMessage()
        {
            var alive = DirectorCore.LastAlive;
            var dead  = DirectorCore.LastDead;
            if (alive.Count == 0 && dead.Count == 0)
                return "<color=#ffd23f>FIN</color> | Aucune partie précédente.";
            string s = alive.Count > 0 ? string.Join(", ", alive) : "aucun";
            string d = dead.Count  > 0 ? string.Join(", ", dead)  : "aucun";
            return $"<color=#ffd23f>FIN</color> | <color=#00ff88>Vivants</color> : {s} | <color=#ff6b6b>Éliminés</color> : {d} — GG !";
        }

        // ── File d'attente ────────────────────────────────────────────────
        private static readonly Queue<string> _queue = new();

        public static string ApplyDefaultColor(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            // Vérifie si le message a déjà des balises de couleur
            bool hasColorTags = text.Contains("<color=") || text.Contains("</color>");
            
            if (!hasColorTags)
            {
                Plugin.Log?.LogInfo($"[ChatManager] Applying default color to: {text}");
                return $"<color=#ffd23f>{text}</color>";
            }
            
            Plugin.Log?.LogInfo($"[ChatManager] Message already has colors: {text}");
            return text;
        }

        public static void Queue(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _queue.Enqueue(ApplyDefaultColor(text));
            }
        }

        // ── Pompe (timer natif du jeu) ────────────────────────────────────
        public static void Pump(ChatController chat)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            // Auto-GG en lobby avec délai de 3 secondes
            if (DirectorCore.PendingAutoGG && ShipStatus.Instance == null)
            {
                if (!_pendingGGTime.HasValue)
                {
                    _pendingGGTime = Time.time + 3f;
                    Plugin.Log?.LogInfo($"[ChatManager] Auto-GG programmé pour {_pendingGGTime.Value:0.00}s");
                }
                else if (Time.time >= _pendingGGTime.Value)
                {
                    DirectorCore.PendingAutoGG = false;
                    _pendingGGTime = null;
                    Queue(GenerateGGMessage());
                    Plugin.Log?.LogInfo("[ChatManager] Auto-GG envoyé !");
                }
            }
            else
            {
                // Réinitialiser si on n'est plus en attente
                _pendingGGTime = null;
            }

            if (_queue.Count == 0) return;

            float minWait = (ShipStatus.Instance == null) ? 3.0f : 1.5f;
            if (chat.timeSinceLastMessage < minWait) return;

            var speaker = LowestAlive() ?? PlayerControl.LocalPlayer;
            if (speaker == null) return;

            Send(speaker, _queue.Dequeue());
            chat.timeSinceLastMessage = 0f;
        }

        private static void Send(PlayerControl speaker, string msg)
        {
            msg = ApplyDefaultColor(msg);
            bool inLobby = ShipStatus.Instance == null;

            if (inLobby)
            {
                // Lobby : Envoie le message à TOUS les joueurs (hôte et autres) via targeted RPC
                // pour préserver les couleurs
                Plugin.Log?.LogInfo($"[ChatManager/Lobby] Sending lobby message to all: {msg}");
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (pc?.Data == null || pc.OwnerId < 0) continue;

                    try
                    {
                        // Envoie un message ciblé à ce joueur
                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, pc.OwnerId);
                        writer.Write(msg);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        
                        // Affiche le message localement pour nous-même aussi
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && HudManager.Instance?.Chat != null)
                        {
                            IsSending = true;
                            HudManager.Instance.Chat.AddChat(speaker, msg);
                            IsSending = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log?.LogError($"[ChatManager/Lobby] Error sending to player {pc.Data.PlayerName}: {e.Message}");
                    }
                }
                return;
            }

            // En partie : paquet atomique avec nom système
            const string sysName = "[ The Director's Cut ]";
            string orig = speaker.Data.PlayerName;
            try
            {
                speaker.SetName(sysName);
                IsSending = true;
                HudManager.Instance.Chat.AddChat(speaker, msg);
                IsSending = false;
                speaker.SetName(orig);
            }
            catch { IsSending = false; speaker.SetName(orig); }

            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(5);
            w.Write(AmongUsClient.Instance.GameId);
            WSetName(w, speaker, sysName);
            WSendChat(w, speaker, msg);
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

        // ── Welcome privé automatique (lobby) ─────────────────────────────
        // Envoie WelcomeMsg uniquement au joueur qui rejoint, les autres ne voient pas.
        private static readonly Dictionary<byte, float> _pendingRules = new();
        private static readonly HashSet<byte>            _sentRules    = new();

        public static void ClearWelcomeSent()
        {
            _pendingRules.Clear();
            _sentRules.Clear();
            Plugin.Log?.LogInfo("[ChatManager] Welcome system cleared!");
        }

        public static void CheckNewPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ShipStatus.Instance != null) return;
            Plugin.Log?.LogInfo($"[ChatManager] CheckNewPlayers() called!");
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                if (_sentRules.Contains(pc.PlayerId) || _pendingRules.ContainsKey(pc.PlayerId)) continue;
                _pendingRules[pc.PlayerId] = Time.time + 3f; // Attend 3 secondes
                Plugin.Log?.LogInfo($"[ChatManager] Welcome planifié pour {pc.Data.PlayerName} (id={pc.PlayerId})!");
            }
        }

        public static void ProcessPendingRules()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var pid in _pendingRules.Keys.ToList())
            {
                Plugin.Log?.LogInfo($"[ChatManager] Checking pending rule for player {pid}: delay elapsed? {Time.time >= _pendingRules[pid]} (Time.time {Time.time:F2}, scheduled {_pendingRules[pid]:F2})");
                if (Time.time < _pendingRules[pid]) continue;
                _pendingRules.Remove(pid);
                PlayerControl target = null;
                foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    if (pc?.PlayerId == pid) { target = pc; break; }
                if (target?.Data != null && target.OwnerId >= 0)
                {
                    _sentRules.Add(pid);
                    SendPrivate(target);
                    Plugin.Log?.LogInfo($"[ChatManager] Welcome sent to {target.Data.PlayerName}!");
                }
                else
                {
                    Plugin.Log?.LogInfo($"[ChatManager] Could not find target for pid {pid}!");
                }
            }
        }

        private static void SendPrivate(PlayerControl target)
        {
            var host = PlayerControl.LocalPlayer;
            if (host == null) return;
            try
            {
                // Ciblé : seul ce joueur reçoit le WelcomeMsg
                var w = AmongUsClient.Instance.StartRpcImmediately(
                    host.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, target.OwnerId);
                w.Write(WelcomeMsg);
                AmongUsClient.Instance.FinishRpcImmediately(w);
                Plugin.Log?.LogInfo($"[ChatManager] Welcome envoyé (privé) → {target.Data.PlayerName}");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[SendPrivate] {e.Message}"); }
        }

        private static void WSetName(MessageWriter w, PlayerControl p, string n)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetName); w.Write(p.Data.NetId); w.Write(n); w.EndMessage(); }
        private static void WSendChat(MessageWriter w, PlayerControl p, string m)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SendChat); w.Write(m); w.EndMessage(); }
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
                chatText = ChatManager.ApplyDefaultColor(chatText);
                Plugin.Log?.LogInfo($"[ChatColor] Applied color to chat text: {chatText}");
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
