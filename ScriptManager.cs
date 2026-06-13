using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using InnerNet;
using AU_TheDirectorsCut.Utils;

namespace AU_TheDirectorsCut
{
    public enum ScriptOrder
    {
        NoReport = 1,
        SkipVote = 2,
        DontUseVents = 3,
        VoteFirst = 4,
        StayOut = 5,
        VoteForPlayer = 6
    }
    
    public enum MapLocation
    {
        
        Skeld_Cafeteria = SystemTypes.Cafeteria,
        Skeld_Admin = SystemTypes.Admin,
        Skeld_Electrical = SystemTypes.Electrical,
        Skeld_Storage = SystemTypes.Storage,
        Skeld_Security = SystemTypes.Security,
        Skeld_Reactor = SystemTypes.Reactor,
        Skeld_UpperEngine = SystemTypes.UpperEngine,
        Skeld_LowerEngine = SystemTypes.LowerEngine,
        Skeld_Medbay = SystemTypes.MedBay,
        Skeld_Communications = SystemTypes.Comms,
        Skeld_Shields = SystemTypes.Shields,
        Skeld_O2 = SystemTypes.LifeSupp,
        Skeld_Navigation = SystemTypes.Nav, 
        Skeld_Weapons = SystemTypes.Weapons
    }

    public class ActiveScript
    {
        public byte PlayerId { get; set; }
        public ScriptOrder Order { get; set; }
        public MapLocation? Location { get; set; }
        public byte? TargetVotePlayerId { get; set; }
        public float Timer { get; set; }
        public bool Active { get; set; }
        public bool Succeeded { get; set; }
    }

    public static class ScriptManager
    {
        private static readonly Dictionary<byte, ActiveScript> ActiveScripts = new Dictionary<byte, ActiveScript>();
        public static Dictionary<byte, ActiveScript>.KeyCollection AllPlayerIds => ActiveScripts.Keys;
        public static IReadOnlyDictionary<byte, ActiveScript> AllScripts => ActiveScripts;
        
        
        public static byte? VoteFirstTargetPlayerId { get; set; }
        public static bool SomeoneVotedBeforeVoteFirst { get; set; }
        public static bool VoteFirstTargetVoted { get; set; }
        public static System.Collections.Generic.List<byte> VotedPlayerIdsInOrder { get; private set; } = new System.Collections.Generic.List<byte>();
        public static System.Collections.Generic.Dictionary<byte, byte> LastKnownVotedFor { get; private set; } = new System.Collections.Generic.Dictionary<byte, byte>();
        
        public static void ResetVoteFirstTracking()
        {
            VoteFirstTargetPlayerId = null;
            SomeoneVotedBeforeVoteFirst = false;
            VoteFirstTargetVoted = false;
            VotedPlayerIdsInOrder.Clear();
            LastKnownVotedFor.Clear();
        }
        
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
            ResetVoteFirstTracking();
            Plugin.Log?.LogInfo("[ScriptManager] Réinitialisé.");
        }

        public static bool AssignScript(byte playerId, ScriptOrder order)
        {
            if (ActiveScripts.ContainsKey(playerId))
            {
                Plugin.Log?.LogInfo($"[ScriptManager] AssignScript failed - player {playerId} already has an active script!");
                return false;
            }

            var script = new ActiveScript
            {
                PlayerId = playerId,
                Order = order,
                Active = true,
                Succeeded = false,
                Timer = float.MaxValue
            };

            ActiveScripts[playerId] = script;
            
            if (order == ScriptOrder.VoteFirst)
            {
                VoteFirstTargetPlayerId = playerId;
                Plugin.Log?.LogInfo($"[ScriptManager] VoteFirst script assigned to player {playerId}");
            }
            
            Plugin.Log?.LogInfo($"[ScriptManager] AssignScript success - player {playerId}, order {order}");
            return true;
        }
        
        public static bool AssignStayOutScript(byte playerId, MapLocation location)
        {
            if (ActiveScripts.ContainsKey(playerId))
            {
                Plugin.Log?.LogInfo($"[ScriptManager] AssignStayOutScript failed - player {playerId} already has an active script!");
                return false;
            }

            var script = new ActiveScript
            {
                PlayerId = playerId,
                Order = ScriptOrder.StayOut,
                Location = location,
                Active = true,
                Succeeded = false,
                Timer = float.MaxValue
            };

            ActiveScripts[playerId] = script;
            Plugin.Log?.LogInfo($"[ScriptManager] AssignStayOutScript success - player {playerId}, location {location}");
            return true;
        }
        
        public static bool AssignVoteForPlayerScript(byte playerId, byte targetVotePlayerId)
        {
            if (ActiveScripts.ContainsKey(playerId))
            {
                Plugin.Log?.LogInfo($"[ScriptManager] AssignVoteForPlayerScript failed - player {playerId} already has an active script!");
                return false;
            }

            var script = new ActiveScript
            {
                PlayerId = playerId,
                Order = ScriptOrder.VoteForPlayer,
                TargetVotePlayerId = targetVotePlayerId,
                Active = true,
                Succeeded = false,
                Timer = float.MaxValue
            };

            ActiveScripts[playerId] = script;
            Plugin.Log?.LogInfo($"[ScriptManager] AssignVoteForPlayerScript success - player {playerId}, targetVotePlayerId {targetVotePlayerId}");
            return true;
        }

        public static bool HasScript(byte playerId) 
        {
            bool has = ActiveScripts.ContainsKey(playerId);
            Plugin.Log?.LogInfo($"[ScriptManager] HasScript({playerId}) → {has}");
            return has;
        }
        
        public static bool HasScript(byte playerId, ScriptOrder order) 
        {
            bool has = ActiveScripts.TryGetValue(playerId, out var s) && s.Order == order && s.Active;
            Plugin.Log?.LogInfo($"[ScriptManager] HasScript({playerId}, {order}) → {has} (Found script: {(s != null ? $"{s.Order}, Active: {s.Active}" : "null")})");
            return has;
        }
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

        private static bool IsInLocation(PlayerControl player, MapLocation location)
        {
            if (player == null || ShipStatus.Instance == null) 
            {
                Plugin.Log?.LogInfo($"[ScriptManager] IsInLocation: player or ShipStatus is null");
                return false;
            }
            
            Vector2 pos = player.GetTruePosition();
            Plugin.Log?.LogInfo($"[ScriptManager] IsInLocation checking {player.Data.PlayerName} at {pos} in {location}");

            
            SystemTypes targetRoomId = (SystemTypes)location;

            
            foreach (PlainShipRoom room in ShipStatus.Instance.AllRooms)
            {
                if (room == null) continue;

                
                if (room.roomArea.OverlapPoint(pos))
                {
                    if (room.RoomId == targetRoomId)
                    {
                        Plugin.Log?.LogInfo($"[ScriptManager] IsInLocation: player is in {targetRoomId}!");
                        return true;
                    }
                }
            }

            Plugin.Log?.LogInfo($"[ScriptManager] IsInLocation: player not in {targetRoomId}");
            return false;
        }

        public static string GetLocationName(MapLocation location)
        {
            return location switch
            {
                MapLocation.Skeld_Cafeteria => "Cafétéria",
                MapLocation.Skeld_Admin => "Admin",
                MapLocation.Skeld_Electrical => "Electrical",
                MapLocation.Skeld_Storage => "Storage",
                MapLocation.Skeld_Security => "Security",
                MapLocation.Skeld_Reactor => "Réacteur",
                MapLocation.Skeld_UpperEngine => "Upper Engine",
                MapLocation.Skeld_LowerEngine => "Lower Engine",
                MapLocation.Skeld_Medbay => "Medbay",
                MapLocation.Skeld_Communications => "Communications",
                MapLocation.Skeld_Shields => "Shields",
                MapLocation.Skeld_O2 => "O2",
                MapLocation.Skeld_Navigation => "Navigation",
                MapLocation.Skeld_Weapons => "Weapons",
                _ => "Zone inconnue"
            };
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance?.AmHost == true) return;

            foreach (var kvp in ActiveScripts.ToList())
            {
                var script = kvp.Value;
                if (!script.Active) continue;

                if (script.Order == ScriptOrder.StayOut && script.Location.HasValue)
                {
                    var player = FindById(script.PlayerId);
                    if (player != null)
                    {
                        if (IsInLocation(player, script.Location.Value))
                        {
                            PunishPlayer(player);
                            script.Active = false;
                        }
                    }
                }
            }

            foreach (var kvp in ActiveScripts.Where(k => !k.Value.Active).ToList())
            {
                ActiveScripts.Remove(kvp.Key);
            }
        }

        public static void AnnounceSuccess(PlayerControl player)
        {
            if (player == null) return;
            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} a respecté son ordre !");
            ChatManager.Queue($"<color=#00ff00>{player.Data.PlayerName} a respecté son ordre !</color>", $"{player.Data.PlayerName} a respecté son ordre !");
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

        public static void PunishPlayer(PlayerControl player)
        {
            if (player == null || player.Data.IsDead) return;

            Plugin.Log?.LogInfo($"[ScriptManager] Punition: {player.Data.PlayerName} a désobéi !");
            DirectorCore.AddPendingPunishment(player);
        }

        private static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        public static string GetOrderName(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => "Ne pas report de corps",
                ScriptOrder.SkipVote => "Skip le prochain vote",
                ScriptOrder.DontUseVents => "Ne pas utiliser les vents",
                ScriptOrder.StayOut => "Ne pas aller dans une zone",
                ScriptOrder.VoteForPlayer => "Voter pour un joueur spécifique",
                _ => "Ordre inconnu"
            };
        }

        public static (string plain, string colored) GetOrderPrivateMessages(ScriptOrder order, string playerName)
        {
            return order switch
            {
                ScriptOrder.NoReport => (
                    $"ORDRE POUR {playerName} : Ne rapporte pas de corps ce round !",
                    $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Ne rapporte pas de corps ce round !"
                ),
                ScriptOrder.SkipVote => (
                    $"ORDRE POUR {playerName} : Passe ton vote ce round !",
                    $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Passe ton vote ce round !"
                ),
                ScriptOrder.DontUseVents => (
                    $"ORDRE POUR {playerName} : Ne pas utiliser les vents ce round !",
                    $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Ne pas utiliser les vents ce round !"
                ),
                ScriptOrder.VoteFirst => (
                    $"ORDRE POUR {playerName} : Tu dois voter EN PREMIER ce round !",
                    $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Tu dois voter EN PREMIER ce round !"
                ),
                _ => ($"ORDRE POUR {playerName} : Suivre un ordre !", $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Suivre un ordre !")
            };
        }
        
        public static (string plain, string colored) GetStayOutPrivateMessages(MapLocation location, string playerName)
        {
            string plain = $"ORDRE POUR {playerName} : Ne vas pas dans {GetLocationName(location)} ce round !";
            string colored = $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Ne vas pas dans {GetLocationName(location)} ce round !";
            return (plain, colored);
        }
        
        public static (string plain, string colored) GetVoteForPlayerPrivateMessages(string targetVoteName, string playerName)
        {
            string plain = $"ORDRE POUR {playerName} : Vote pour {targetVoteName} ce round !";
            string colored = $"<color=#ffd23f>ORDRE POUR {playerName}</color>: Vote pour {targetVoteName} ce round !";
            return (plain, colored);
        }
    }
}
