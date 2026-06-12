using Hazel;
using System;

namespace HydraMenu.anticheat.rpc
{
	internal class EnterVent : RpcCheck
	{
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			if(ShipStatus.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to vent when there is no instance of ShipStatus.");
				blockRpc = true;
				return;
			}

			
			
			
			if(!player.Data.IsDead && !player.Data.Role.CanVent)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to vent when their role ({player.Data.RoleType}) does not support venting.");
				blockRpc = true;
				return;
			}

			if(GameManager.Instance.IsHideAndSeek() && RoleManager.IsImpostorRole(player.Data.RoleType))
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to vent while being the seeker.");
				blockRpc = true;
				return;
			}
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.EnterVent;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(PlayerPhysics);
		}
	}
}