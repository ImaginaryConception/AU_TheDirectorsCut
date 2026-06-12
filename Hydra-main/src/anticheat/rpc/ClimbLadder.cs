using Hazel;
using System;

namespace HydraMenu.anticheat.rpc
{
	internal class ClimbLadder : RpcCheck
	{
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			if(ShipStatus.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to climb a ladder when there is no instance of ShipStatus.");
				blockRpc = true;
				return;
			}

			MapNames map = Utilities.GetCurrentMap();
			if(map != MapNames.Airship && map != MapNames.Fungle)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to climb a ladder outside of the proper map.");
				blockRpc = true;
				return;
			}

			
			
			
			if(player.Data.IsDead)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to climb a ladder while dead.");
				blockRpc = true;
				return;
			}
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.ClimbLadder;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(PlayerPhysics);
		}
	}
}