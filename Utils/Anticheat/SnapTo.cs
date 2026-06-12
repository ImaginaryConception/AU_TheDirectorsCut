using Hazel;
using System;
using UnityEngine;

namespace AU_TheDirectorsCut.Utils.Anticheat
{
    internal class SnapTo : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            Vector2 position = NetHelpers.ReadVector2(reader);
            

            if (LobbyBehaviour.Instance != null)
            {
                Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SnapTo RPC while inside the lobby.");
                blockRpc = true;
            }

            
            if (blockRpc && (AmongUsClient.Instance.AmHost && !Utilities.IsAnticheatPresent()))
            {
                player.NetTransform.RpcSnapTo(player.transform.position);
            }
        }

        public override RpcCalls GetRpcCall()
        {
            return RpcCalls.SnapTo;
        }

        public override Type GetExpectedNetObject()
        {
            return typeof(CustomNetworkTransform);
        }
    }
}
