using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using InnerNet;
using AU_TheDirectorsCut.Hydra;

namespace AU_TheDirectorsCut
{
    public enum ScriptOrder
    {
        NoReport = 1,
        StayStill = 2,
        SkipVote = 3,
        GoToAdmin = 4,
        GoToElectrical = 5,
        FixLights = 6,
        VoteBlue = 7,
        VoteRed = 8,
        DontUseVents = 9,
        SayPlayerIsSafe = 10
    }

    public class ActiveScript
    {
        public byte PlayerId { get; set; }
        public ScriptOrder Order { get; set; }
        public Vector2? InitialPosition { get; set; }
        public float Timer { get; set; }
        public bool Active { get; set; }
        public bool Succeeded { get; set; }
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
                return false;
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
                    ScriptOrder.GoToAdmin => 20f,
                    ScriptOrder.GoToElectrical => 20f,
                    ScriptOrder.FixLights => 30f,
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

        private static bool IsPlayerInRoom(PlayerControl player, Vector2 min, Vector2 max)
        {
            if (player == null || ShipStatus.Instance == null) return false;
            
            Vector2 pos = player.GetTruePosition();
            return pos.x > min.x && pos.x < max.x && pos.y > min.y && pos.y < max.y;
        }

        private static bool IsPlayerInAdmin(PlayerControl player)
        {
            if (player == null || ShipStatus.Instance == null) return false;
            
            // The Skeld: Exact Admin bounds from map data
            Vector2 pos = player.GetTruePosition();
            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} checking Admin at (X: {pos.x:F2}, Y: {pos.y:F2})");
            
            // The Skeld Admin room is around:
            // X from 2.8 to 8.5
            // Y from -16.5 to -11.0
            bool isInSkeldAdmin = pos.x > 2.8f && pos.x < 8.5f && pos.y > -16.5f && pos.y < -11.0f;
            
            if (isInSkeldAdmin)
            {
                Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} IS IN ADMIN!");
            }
            
            return isInSkeldAdmin;
        }

        private static bool AreLightsOff()
        {
            if (ShipStatus.Instance == null) return false;
            
            var switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].TryCast<SwitchSystem>();
            if (switchSystem != null)
            {
                return !switchSystem.IsActive;
            }
            
            return false; // If we can't check, assume lights are on
        }

        private static bool IsPlayerInElectrical(PlayerControl player)
        {
            if (player == null || ShipStatus.Instance == null) return false;
            
            // The Skeld: TIGHT Electrical bounds (definitely not Cargo!)
            Vector2 pos = player.GetTruePosition();
            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} at (X: {pos.x:F3}, Y: {pos.y:F3})");
            
            // The Skeld Electrical only - very small, no overlap with Cargo!
            // X from -6.5 to 0.5
            // Y from -16.8 to -10.0
            bool isInSkeldElectrical = pos.x > -6.5f && pos.x < 0.5f && pos.y > -16.8f && pos.y < -10.0f;
            
            if (isInSkeldElectrical)
            {
                Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} IS IN ELECTRICAL!");
            }
            
            return isInSkeldElectrical;
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance?.AmHost == true) return;

            foreach (var kvp in ActiveScripts.ToList())
            {
                var script = kvp.Value;
                if (!script.Active) continue;

                switch (script.Order)
                {
                    case ScriptOrder.StayStill:
                        script.Timer -= Time.deltaTime;
                        if (script.Timer <= 0f)
                        {
                            script.Active = false;
                            script.Succeeded = true;
                            AnnounceSuccess(FindById(script.PlayerId));
                        }
                        else
                        {
                            var player = FindById(script.PlayerId);
                            if (player != null && script.InitialPosition.HasValue)
                            {
                                Vector2 currentPos = (Vector2)player.transform.position;
                                float distance = Vector2.Distance(script.InitialPosition.Value, currentPos);
                                if (distance > 0.5f)
                                {
                                    PunishPlayer(player);
                                    script.Active = false;
                                }
                            }
                        }
                        break;
                        
                    case ScriptOrder.GoToAdmin:
                        script.Timer -= Time.deltaTime;
                        var adminPlayer = FindById(script.PlayerId);
                        if (adminPlayer != null)
                        {
                            if (IsPlayerInAdmin(adminPlayer))
                            {
                                script.Active = false;
                                script.Succeeded = true;
                                AnnounceSuccess(adminPlayer);
                            }
                            else if (script.Timer <= 0f)
                            {
                                PunishPlayer(adminPlayer);
                                script.Active = false;
                            }
                        }
                        break;
                        
                    case ScriptOrder.GoToElectrical:
                        script.Timer -= Time.deltaTime;
                        var elecPlayer = FindById(script.PlayerId);
                        if (elecPlayer != null)
                        {
                            if (IsPlayerInElectrical(elecPlayer))
                            {
                                script.Active = false;
                                script.Succeeded = true;
                                AnnounceSuccess(elecPlayer);
                            }
                            else if (script.Timer <= 0f)
                            {
                                PunishPlayer(elecPlayer);
                                script.Active = false;
                            }
                        }
                        break;
                        
                    case ScriptOrder.FixLights:
                        // First check if lights are even off!
                        if (!AreLightsOff())
                        {
                            // Lights are already on - auto-complete!
                            script.Active = false;
                            script.Succeeded = true;
                            var autoPlayer = FindById(script.PlayerId);
                            if (autoPlayer != null)
                            {
                                AnnounceSuccess(autoPlayer);
                            }
                        }
                        else
                        {
                            // Lights are off - wait for player to fix!
                            script.Timer -= Time.deltaTime;
                            var fixPlayer = FindById(script.PlayerId);
                            if (fixPlayer != null)
                            {
                                // Check if lights are now fixed AND player is in Electrical
                                if (!AreLightsOff() && IsPlayerInElectrical(fixPlayer))
                                {
                                    script.Active = false;
                                    script.Succeeded = true;
                                    AnnounceSuccess(fixPlayer);
                                }
                                else if (script.Timer <= 0f)
                                {
                                    PunishPlayer(fixPlayer);
                                    script.Active = false;
                                }
                            }
                        }
                        break;
                }
            }

            foreach (var kvp in ActiveScripts.Where(k => !k.Value.Active).ToList())
            {
                ActiveScripts.Remove(kvp.Key);
            }
        }

        private static void AnnounceSuccess(PlayerControl player)
        {
            if (player == null) return;
            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} a respecté son ordre !");
            ChatManager.Queue($"<color=#00ff00>{player.Data.PlayerName} a respecté son ordre !</color>", $"{player.Data.PlayerName} a respecté son ordre !");
        }

        public static void PunishPlayer(PlayerControl player)
        {
            if (player == null || player.Data.IsDead) return;

            Plugin.Log?.LogInfo($"[ScriptManager] Punition: {player.Data.PlayerName} a désobéi !");
            
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
            {
                PlayerControl.LocalPlayer.RpcMurderPlayer(player, true);
            }

            ChatManager.Queue($"<color=#ff6b6b>{player.Data.PlayerName} a désobéi au script — éliminé !</color>", $"{player.Data.PlayerName} a désobéi au script — éliminé !");
        }

        private static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        public static string GetOrderName(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => "Ne pas rapporter de corps",
                ScriptOrder.StayStill => "Rester immobile 10s",
                ScriptOrder.SkipVote => "Skip le prochain vote",
                ScriptOrder.GoToAdmin => "Aller en Admin",
                ScriptOrder.GoToElectrical => "Aller en Electrical",
                ScriptOrder.FixLights => "Réparer les lumières",
                ScriptOrder.VoteBlue => "Voter pour le bleu",
                ScriptOrder.VoteRed => "Voter pour le rouge",
                ScriptOrder.DontUseVents => "Ne pas utiliser les vents",
                ScriptOrder.SayPlayerIsSafe => "Dire que quelqu'un est safe",
                _ => "Ordre inconnu"
            };
        }

        public static string GetOrderPrivateMessage(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => "Tu dois ne pas rapporter de corps ce round ! Sinon tu meurs.",
                ScriptOrder.StayStill => "Tu dois rester immobile 10 secondes ! Sinon tu meurs.",
                ScriptOrder.SkipVote => "Tu dois skip le prochain vote ! Sinon tu meurs.",
                ScriptOrder.GoToAdmin => "Tu dois aller en Admin dans les 20 secondes ! Sinon tu meurs.",
                ScriptOrder.GoToElectrical => "Tu dois aller en Electrical dans les 20 secondes ! Sinon tu meurs.",
                ScriptOrder.FixLights => "Tu dois réparer les lumières si elles sont éteintes ! Sinon tu meurs.",
                ScriptOrder.VoteBlue => "Tu dois voter pour le joueur bleu ce meeting ! Sinon tu meurs.",
                ScriptOrder.VoteRed => "Tu dois voter pour le joueur rouge ce meeting ! Sinon tu meurs.",
                ScriptOrder.DontUseVents => "Tu ne dois pas utiliser les vents ce round ! Sinon tu meurs.",
                ScriptOrder.SayPlayerIsSafe => "Tu dois dire dans le chat que quelqu'un est innocent ! Sinon tu meurs.",
                _ => "Tu dois suivre un ordre ! Sinon tu meurs."
            };
        }
    }
}
