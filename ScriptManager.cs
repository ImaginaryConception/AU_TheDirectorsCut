using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AU_TheDirectorsCut.Hydra;

namespace AU_TheDirectorsCut
{
    public enum ScriptOrder
    {
        NoReport = 1,
        StayStill = 2,
        SkipVote = 3,
        GoToAdmin = 4
    }

    public class ActiveScript
    {
        public byte PlayerId { get; set; }
        public ScriptOrder Order { get; set; }
        public Vector2? InitialPosition { get; set; }
        public float Timer { get; set; }
        public bool Active { get; set; }
        public bool Succeeded { get; set; } // Nouveau: pour marquer si l'ordre a été respecté
    }

    public static class ScriptManager
    {
        private static readonly Dictionary<byte, ActiveScript> ActiveScripts = new Dictionary<byte, ActiveScript>();
        public static Dictionary<byte, ActiveScript>.KeyCollection AllPlayerIds => ActiveScripts.Keys;
        public static IReadOnlyDictionary<byte, ActiveScript> AllScripts => ActiveScripts;
        
        public static List<KeyValuePair<byte, ActiveScript>> GetAllActiveScripts()
        {
            return ActiveScripts.ToList();
        }

        public static void Initialize()
        {
            Plugin.Log?.LogInfo("[ScriptManager] Initialisé.");
        }

        public static void Reset()
        {
            ActiveScripts.Clear();
            Plugin.Log?.LogInfo("[ScriptManager] Réinitialisé.");
        }

        public static bool AssignScript(byte playerId, ScriptOrder order)
        {
            if (ActiveScripts.ContainsKey(playerId))
            {
                return false; // Déjà un script actif
            }

            var script = new ActiveScript
            {
                PlayerId = playerId,
                Order = order,
                Active = true,
                Succeeded = false,
                Timer = order switch
                {
                    ScriptOrder.StayStill => 10f,
                    ScriptOrder.GoToAdmin => 15f,
                    _ => float.MaxValue
                }
            };

            if (order == ScriptOrder.StayStill)
            {
                var player = FindById(playerId);
                if (player != null)
                {
                    script.InitialPosition = (Vector2)player.transform.position;
                }
            }

            ActiveScripts[playerId] = script;
            Plugin.Log?.LogInfo($"[ScriptManager] Script assigné à {playerId}: {order}");
            return true;
        }

        public static bool HasScript(byte playerId) => ActiveScripts.ContainsKey(playerId);
        public static bool HasScript(byte playerId, ScriptOrder order) => ActiveScripts.TryGetValue(playerId, out var s) && s.Order == order && s.Active;
        public static void RemoveScript(byte playerId)
        {
            if (ActiveScripts.Remove(playerId))
            {
                Plugin.Log?.LogInfo($"[ScriptManager] Script retiré de {playerId}");
            }
        }

        private static bool IsPlayerInAdmin(PlayerControl player)
        {
            if (player == null || ShipStatus.Instance == null) return false;
            
            // Check position roughly for all maps - MUCH BIGGER zones!
            Vector2 pos = player.GetTruePosition();
            
            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} position: x={pos.x}, y={pos.y}");
            
            // The Skeld Admin is roughly at (6.5, -15.5)
            if (pos.x > 0f && pos.x < 12f && pos.y < -8f && pos.y > -20f)
            {
                Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} est dans Admin (Skeld)!");
                return true;
            }
            
            // Mira HQ Admin is roughly at (22, 2)
            if (pos.x > 15f && pos.x < 29f && pos.y > -4f && pos.y < 8f)
            {
                Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} est dans Admin (Mira)!");
                return true;
            }
            
            // Polus Admin is roughly at (18, -17)
            if (pos.x > 10f && pos.x < 26f && pos.y < -10f && pos.y > -24f)
            {
                Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} est dans Admin (Polus)!");
                return true;
            }
            
            // Airship Admin is roughly at (13, 13)
            if (pos.x > 5f && pos.x < 21f && pos.y > 6f && pos.y < 20f)
            {
                Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} est dans Admin (Airship)!");
                return true;
            }

            return false;
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance?.AmHost == true) return;

            foreach (var kvp in ActiveScripts.ToList())
            {
                var script = kvp.Value;
                if (!script.Active) continue;

                if (script.Order == ScriptOrder.StayStill)
                {
                    script.Timer -= Time.deltaTime;
                    if (script.Timer <= 0f)
                    {
                        // Réussi !
                        script.Active = false;
                        script.Succeeded = true;
                        Plugin.Log?.LogInfo($"[ScriptManager] {script.PlayerId} a respecté StayStill !");
                    }
                    else
                    {
                        // Vérifier le mouvement
                        var player = FindById(script.PlayerId);
                        if (player != null && script.InitialPosition.HasValue)
                        {
                            Vector2 currentPos = (Vector2)player.transform.position;
                            float distance = Vector2.Distance(script.InitialPosition.Value, currentPos);
                            if (distance > 0.5f)
                            {
                                // A bougé !
                                PunishPlayer(player);
                                script.Active = false;
                            }
                        }
                    }
                }
                else if (script.Order == ScriptOrder.GoToAdmin)
                {
                    script.Timer -= Time.deltaTime;
                    var player = FindById(script.PlayerId);
                    
                    if (player != null)
                    {
                        // Check EVERY FRAME if player is in Admin - mark as succeeded as soon as they enter!
                        if (IsPlayerInAdmin(player))
                        {
                            // Réussi !
                            script.Active = false;
                            script.Succeeded = true;
                            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} a rejoint Admin à temps !");
                        }
                        else if (script.Timer <= 0f)
                        {
                            // Temps écoulé et pas dans Admin
                            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} n'a pas rejoint Admin à temps ! Timer écoulé.");
                            PunishPlayer(player);
                            script.Active = false;
                        }
                    }
                }
            }

            // Nettoyer les scripts inactifs
            foreach (var kvp in ActiveScripts.Where(k => !k.Value.Active).ToList())
            {
                ActiveScripts.Remove(kvp.Key);
            }
        }

        public static void PunishPlayer(PlayerControl player)
        {
            if (player == null || player.Data.IsDead) return;

            Plugin.Log?.LogInfo($"[ScriptManager] Punition: {player.Data.PlayerName} a désobéi !");
            
            // Utiliser la même méthode que Hydra /cut
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
            {
                PlayerControl.LocalPlayer.RpcMurderPlayer(player, true);
            }

            ChatManager.Queue($"<color=#ff6b6b>{player.Data.PlayerName}</color> a désobéi au script — éliminé !", $"{player.Data.PlayerName} a désobéi au script — éliminé !");
        }

        private static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        public static string GetOrderName(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => "Ne pas report de corps",
                ScriptOrder.StayStill => "Rester immobile 10s",
                ScriptOrder.SkipVote => "Skip le prochain vote",
                ScriptOrder.GoToAdmin => "Etre dans Admin au bout de 15s",
                _ => "Ordre inconnu"
            };
        }

        public static string GetOrderPrivateMessage(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => "Tu dois ne rapporter aucun corps. Sinon, tu meurs.",
                ScriptOrder.StayStill => "Tu dois rester immobile 10 secondes. Sinon, tu meurs.",
                ScriptOrder.SkipVote => "Tu dois passer ton prochain vote. Sinon, tu meurs.",
                ScriptOrder.GoToAdmin => "Tu dois etre dans la salle Admin au bout de 15 secondes. Sinon, tu meurs.",
                _ => "Tu dois suivre un ordre. Sinon, tu meurs."
            };
        }
    }
}
