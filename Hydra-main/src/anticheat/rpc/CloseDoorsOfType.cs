using Hazel;
using System;

namespace HydraMenu.anticheat.rpc
{
	internal class CloseDoorsOfType : RpcCheck
	{
		
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			
			
			if(GameManager.Instance.IsHideAndSeek())
			{
				Hydra.notifications.Send("Anticheat", "Someone attempted to close doors while in Hide and Seek.", Anticheat.NotificationDuration);
				blockRpc = true;
			}
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.CloseDoorsOfType;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(ShipStatus);
		}
	}
}