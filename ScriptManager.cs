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
        SkipVote = 2,
        DontUseVents = 3,
        SayPlayerIsSafe = 4,
        StayOut = 5,
        VoteForPlayer = 6
    }
    
    public enum MapLocation
    {
        // The Skeld (mapped to SystemTypes)
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
        Skeld_Navigation = SystemTypes.Nav, // Fixed!
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
                Timer = float.MaxValue
            };

            ActiveScripts[playerId] = script;
            Plugin.Log?.LogInfo($"[ScriptManager] Script assigné à {playerId}: {order}");
            return true;
        }
        
        public static bool AssignStayOutScript(byte playerId, MapLocation location)
        {
            if (ActiveScripts.ContainsKey(playerId))
            {
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
            Plugin.Log?.LogInfo($"[ScriptManager] StayOut script assigné à {playerId}: {location}");
            return true;
        }
        
        public static bool AssignVoteForPlayerScript(byte playerId, byte targetVotePlayerId)
        {
            if (ActiveScripts.ContainsKey(playerId))
            {
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
            Plugin.Log?.LogInfo($"[ScriptManager] VoteForPlayer script assigné à {playerId}: vote for {targetVotePlayerId}");
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

        private static bool IsInLocation(PlayerControl player, MapLocation location)
        {
            if (player == null || ShipStatus.Instance == null) 
            {
                Plugin.Log?.LogInfo($"[ScriptManager] IsInLocation: player or ShipStatus is null");
                return false;
            }
            
            Vector2 pos = player.GetTruePosition();
            Plugin.Log?.LogInfo($"[ScriptManager] IsInLocation checking {player.Data.PlayerName} at {pos} in {location}");

            // Target room is our MapLocation cast to SystemTypes
            SystemTypes targetRoomId = (SystemTypes)location;

            // Check all rooms to find the one the player is in
            foreach (PlainShipRoom room in ShipStatus.Instance.AllRooms)
            {
                if (room == null) continue;

                // Check if the player's position is inside the room's collider
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

            // Handle host position lock (from HydraKillPlayer)
            if (_hostPositionLockTimer > 0f && _savedHostPosition.HasValue && PlayerControl.LocalPlayer != null)
            {
                _hostPositionLockTimer -= Time.deltaTime;
                PlayerControl.LocalPlayer.transform.position = _savedHostPosition.Value;
            }

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

        private static void AnnounceSuccess(PlayerControl player)
        {
            if (player == null) return;
            Plugin.Log?.LogInfo($"[ScriptManager] {player.Data.PlayerName} a respecté son ordre !");
            ChatManager.Queue($"<color=#00ff00>{player.Data.PlayerName} a respecté son ordre !</color>", $"{player.Data.PlayerName} a respecté son ordre !");
        }

        private static Vector3? _savedHostPosition;
        private static float _hostPositionLockTimer;

        private static void HydraKillPlayer(PlayerControl target)
        {
            // Use Hydra's method to kill the player, exactly like in PlayersSection.AttemptMurder
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
            {
                // Save host's position before killing
                _savedHostPosition = PlayerControl.LocalPlayer.transform.position;
                _hostPositionLockTimer = 0.3f; // Lock position for 0.3 seconds

                // Kill the target
                PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);

                // Immediately teleport host back to saved position
                if (_savedHostPosition.HasValue)
                {
                    PlayerControl.LocalPlayer.transform.position = _savedHostPosition.Value;
                }
            }
        }

        public static void PunishPlayer(PlayerControl player)
        {
            if (player == null || player.Data.IsDead) return;

            Plugin.Log?.LogInfo($"[ScriptManager] Punition: {player.Data.PlayerName} a désobéi !");
            
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
            {
                HydraKillPlayer(player);
            }

            ChatManager.Queue($"<color=#ff6b6b>{player.Data.PlayerName} a désobéi au script — éliminé !</color>", $"{player.Data.PlayerName} a désobéi au script — éliminé !");
        }

        private static PlayerControl FindById(byte id) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        public static string GetOrderName(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => "Ne pas rapporter de corps",
                ScriptOrder.SkipVote => "Skip le prochain vote",
                ScriptOrder.DontUseVents => "Ne pas utiliser les vents",
                ScriptOrder.SayPlayerIsSafe => "Dire que quelqu'un est safe",
                ScriptOrder.StayOut => "Ne pas aller dans une zone",
                ScriptOrder.VoteForPlayer => "Voter pour un joueur spécifique",
                _ => "Ordre inconnu"
            };
        }

        public static (string plain, string colored) GetOrderPrivateMessages(ScriptOrder order)
        {
            return order switch
            {
                ScriptOrder.NoReport => (
                    "Tu ne dois pas rapporter de corps ce round !",
                    "<color=#ff6b6b>ORDRE</color>: Tu ne dois pas rapporter de corps ce round !"
                ),
                ScriptOrder.SkipVote => (
                    "Tu dois passer ton vote ce round !",
                    "<color=#ff6b6b>ORDRE</color>: Tu dois passer ton vote ce round !"
                ),
                ScriptOrder.DontUseVents => (
                    "Tu ne dois pas utiliser les vents ce round !",
                    "<color=#ff6b6b>ORDRE</color>: Tu ne dois pas utiliser les vents ce round !"
                ),
                ScriptOrder.SayPlayerIsSafe => (
                    "Tu dois dire que quelqu'un est innocent !",
                    "<color=#ff6b6b>ORDRE</color>: Tu dois dire que quelqu'un est innocent !"
                ),
                _ => ("Tu dois suivre un ordre !", "<color=#ff6b6b>ORDRE</color>: Tu dois suivre un ordre !")
            };
        }
        
        public static (string plain, string colored) GetStayOutPrivateMessages(MapLocation location)
        {
            string plain = $"Tu ne dois pas aller dans {GetLocationName(location)} ce round !";
            string colored = $"<color=#ff6b6b>ORDRE</color>: Tu ne dois pas aller dans {GetLocationName(location)} ce round !";
            return (plain, colored);
        }
        
        public static (string plain, string colored) GetVoteForPlayerPrivateMessages(string targetPlayerName)
        {
            string plain = $"Tu dois voter pour {targetPlayerName} ce round !";
            string colored = $"<color=#ff6b6b>ORDRE</color>: Tu dois voter pour {targetPlayerName} ce round !";
            return (plain, colored);
        }
    }
}
