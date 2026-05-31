using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    public static class NetworkManager
    {
        public static void Initialize()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] Initialized completely!");
        }

        public static void SendCutSignal()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] SendCutSignal called!");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Not host, can't send cut signal!");
                return;
            }

            try
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Sending cut visual/audio signals!");
                
                // Trigger reactor sabotage for red light effect - try multiple methods
                if (ShipStatus.Instance != null)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[NetworkManager] Activating reactor visual!");
                    
                    // Method 1: UpdateSystem (what we tried)
                    try
                    {
                        ShipStatus.Instance.UpdateSystem(SystemTypes.Reactor, PlayerControl.LocalPlayer, 128);
                        if (Plugin.Log != null)
                            Plugin.Log.LogInfo("[NetworkManager] Method 1 (UpdateSystem 128) worked!");
                    }
                    catch (Exception e1)
                    {
                        if (Plugin.Log != null)
                            Plugin.Log.LogError($"[NetworkManager] Method 1 failed: {e1}");
                    }
                }
                
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Cut signals sent!");
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError($"[NetworkManager] Error sending cut signal: {e}");
            }
        }

        public static void StopCutSignal()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] StopCutSignal called!");
            
            if (!AmongUsClient.Instance.AmHost) return;
            
            // Try to repair the reactor - try multiple methods
            if (ShipStatus.Instance != null)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Trying to stop reactor...");
                
                // Method 1: Try with 0
                try
                {
                    ShipStatus.Instance.UpdateSystem(SystemTypes.Reactor, PlayerControl.LocalPlayer, 0);
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[NetworkManager] Method 1 (UpdateSystem 0) worked!");
                }
                catch (Exception e1)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError($"[NetworkManager] Method 1 failed: {e1}");
                }

                // Method 2: Try with 16
                try
                {
                    ShipStatus.Instance.UpdateSystem(SystemTypes.Reactor, PlayerControl.LocalPlayer, 16);
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[NetworkManager] Method 2 (UpdateSystem 16) worked!");
                }
                catch (Exception e2)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError($"[NetworkManager] Method 2 failed: {e2}");
                }
            }
        }

        public static void SwapPlayers(PlayerControl p1, PlayerControl p2)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] SwapPlayers called!");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Not host, can't swap players!");
                return;
            }

            if (p1 == null || p2 == null) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] One or both players are null!");
                return;
            }

            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Swapping players {p1.Data.PlayerName} (ID: {p1.PlayerId}) and {p2.Data.PlayerName} (ID: {p2.PlayerId})");

            try
            {
                Vector2 pos1 = p1.GetTruePosition();
                Vector2 pos2 = p2.GetTruePosition();

                if (Plugin.Log != null)
                    Plugin.Log.LogInfo($"[NetworkManager] Pos1: {pos1}, Pos2: {pos2}");

                p1.NetTransform.SnapTo(pos2);
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Player1 snapped!");
                
                p2.NetTransform.SnapTo(pos1);
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Player2 snapped!");

                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Swap completed!");
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError($"[NetworkManager] Error swapping players: {e}");
            }
        }

        public static void SetGameSpeed(float speedMultiplier)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] SetGameSpeed called!");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Not host, can't set game speed!");
                return;
            }

            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Setting game speed to {speedMultiplier}");
        }

        public static void MurderPlayer(PlayerControl target)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] MurderPlayer called!");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Not host, can't murder player!");
                return;
            }

            if (target == null) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Target player is null!");
                return;
            }

            if (target.Data.IsDead) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Target player is already dead!");
                return;
            }

            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Murdering player {target.Data.PlayerName} (ID: {target.PlayerId})");

            try
            {
                target.Die(DeathReason.Kill, true);
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[NetworkManager] Murder completed!");
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError($"[NetworkManager] Error murdering player: {e}");
            }
        }

        public static void BlindPlayer(byte targetId)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] BlindPlayer called!");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[NetworkManager] Not host, can't blind player!");
                return;
            }

            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Blinding player with ID: {targetId}");
        }

        public static void FreezePlayer(PlayerControl target)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] FreezePlayer called!");
            
            if (!AmongUsClient.Instance.AmHost) return;
            
            if (target == null || target.Data == null) return;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Freezing player {target.Data.PlayerName}!");
        }

        public static void TeleportAllTo(PlayerControl target)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] TeleportAllTo called!");
            
            if (!AmongUsClient.Instance.AmHost || target == null) return;
            
            Vector2 targetPos = target.GetTruePosition();
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Teleporting all players to {target.Data.PlayerName} at {targetPos}!");
            
            foreach (var player in PlayerControl.AllPlayerControls.ToArray())
            {
                if (player != null && player.Data != null && !player.Data.IsDead && !player.Data.Disconnected && player.PlayerId != target.PlayerId)
                {
                    try
                    {
                        player.NetTransform.SnapTo(targetPos);
                        if (Plugin.Log != null)
                            Plugin.Log.LogInfo($"[NetworkManager] Teleported {player.Data.PlayerName}!");
                    }
                    catch (Exception e)
                    {
                        if (Plugin.Log != null)
                            Plugin.Log.LogError($"[NetworkManager] Error teleporting {player.Data.PlayerName}: {e}");
                    }
                }
            }
        }

        public static void ShuffleAllPlayers()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] ShuffleAllPlayers called!");
            
            if (!AmongUsClient.Instance.AmHost) return;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] Shuffling all players!");
            
            var allPlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected).ToList();
            var positions = allPlayers.Select(p => p.GetTruePosition()).ToList();
            
            System.Random rng = new System.Random();
            int n = positions.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Vector2 temp = positions[k];
                positions[k] = positions[n];
                positions[n] = temp;
            }
            
            for (int i = 0; i < allPlayers.Count; i++)
            {
                try
                {
                    allPlayers[i].NetTransform.SnapTo(positions[i]);
                }
                catch (Exception e)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogError($"[NetworkManager] Error shuffling {allPlayers[i].Data.PlayerName}: {e}");
                }
            }
        }

        public static void SpinPlayer(PlayerControl target)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] SpinPlayer called!");
            
            if (!AmongUsClient.Instance.AmHost || target == null) return;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[NetworkManager] Spinning player {target.Data.PlayerName}!");
        }

        public static void BouncyMode()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] BouncyMode called!");
            
            if (!AmongUsClient.Instance.AmHost) return;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] Bouncy mode activated!");
        }

        public static void RandomizeColors()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] RandomizeColors called!");
            
            if (!AmongUsClient.Instance.AmHost) return;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[NetworkManager] Randomizing all player colors!");
        }
    }
}
