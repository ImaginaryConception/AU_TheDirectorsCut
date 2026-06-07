using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;

namespace AU_TheDirectorsCut.Hydra.Anticheat
{
    internal class Anticheat
    {
        public static bool Enabled { get; set; } = true;

        public static Dictionary<RpcCalls, RpcCheck> RpcHandlers = new Dictionary<RpcCalls, RpcCheck>()
        {
            { RpcCalls.SnapTo, new SnapTo() }
        };

        public static bool CheckSpoofedPlatforms { get; set; } = true;

        public enum Punishments
        {
            None,
            Kick,
            ErrorKick,
            Ban
        }

        public static float NotificationDuration = 10.0f;

        public static Punishments punishment = Punishments.None;
        public static bool sendNotification = true;
        public static bool discardRpc = true;

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        class OnPlayerControlRPC
        {
            static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
            {
                return HandleRpc(typeof(PlayerControl), __instance, (RpcCalls)callId, reader);
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
        class OnPlayerPhysicsRPC
        {
            static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
            {
                return HandleRpc(typeof(PlayerPhysics), __instance.myPlayer, (RpcCalls)callId, reader);
            }
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
        class OnNetTransformRPC
        {
            static bool Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
            {
                return HandleRpc(typeof(CustomNetworkTransform), __instance.myPlayer, (RpcCalls)callId, reader);
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
        class OnShipStatusRPC
        {
            static bool Prefix(ShipStatus __instance, byte callId, MessageReader reader)
            {
                return HandleRpc(typeof(ShipStatus), null, (RpcCalls)callId, reader);
            }
        }

        private static bool HandleRpc(Type sourceNetObj, PlayerControl player, RpcCalls rpc, MessageReader reader)
        {
            RpcHandlers.TryGetValue(rpc, out RpcCheck rpcCheck);
            if (!Enabled || rpcCheck == null || !rpcCheck.Enabled) return true;

            if (sourceNetObj != rpcCheck.GetExpectedNetObject())
            {
                return false;
            }

            // Only we, the host, should be sending host-only RPCs
            if (player != null && AmongUsClient.Instance.AmHost && rpcCheck.IsHostOnly())
            {
                Flag(player, $"{player.Data.PlayerName} sent the {rpc} RPC while non-host.");
                return false;
            }

            int oldReadPosition = reader.Position;
            bool blockRpc = false;

            rpcCheck.Validate(player, reader, ref blockRpc);
            if (discardRpc && blockRpc) return false;

            reader.Position = oldReadPosition;
            return true;
        }

        public static void Flag(PlayerControl player, string reason, bool shouldPunish = true)
        {
            // Sanity check, make sure that we are not flagging ourselves
            if (player == PlayerControl.LocalPlayer) return;

            if (sendNotification)
            {
                Plugin.Log?.LogMessage(reason);
            }

            if (AmongUsClient.Instance.AmHost && shouldPunish)
            {
                Punish(player);
            }
        }

        private static void Punish(PlayerControl player)
        {
            switch (punishment)
            {
                case Punishments.None:
                    break;

                case Punishments.Kick:
                case Punishments.ErrorKick:
                    Plugin.Log?.LogMessage($"{player.Data.PlayerName} was kicked by Hydra Anticheat for hacking");

                    // The vanilla anticheat prevents using the ErrorKick method if the game has not started yet
                    if (punishment == Punishments.Kick || LobbyBehaviour.Instance != null)
                    {
                        AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
                    }
                    else
                    {
                        AmongUsClient.Instance.SendLateRejection(player.OwnerId, DisconnectReasons.ClientTimeout);
                    }
                    break;

                case Punishments.Ban:
                    Plugin.Log?.LogMessage($"{player.Data.PlayerName} was automatically banned by Hydra Anticheat for hacking");
                    AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                    break;
            }
        }
    }
}
