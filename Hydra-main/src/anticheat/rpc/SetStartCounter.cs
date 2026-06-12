using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class SetStartCounter : RpcCheck
	{
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			reader.ReadPackedInt32();
			sbyte counter = reader.ReadSByte();

			
			
			if(player.OwnerId != AmongUsClient.Instance.HostId && counter != -1)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent a SetStartCounter RPC with an invalid value: {counter}.");
				blockRpc = true;

				
				if(AmongUsClient.Instance.AmHost)
				{
					PlayerControl.LocalPlayer.RpcSetStartCounter(-1);
				}
			}

			
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.SetStartCounter;
		}
	}
}