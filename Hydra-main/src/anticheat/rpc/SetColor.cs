using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class SetColor : RpcCheck
	{
		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.SetColor;
		}

		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			uint netId = reader.ReadUInt32();
			byte color = reader.ReadByte();

			// This net id field written in the RPC is seemingly useless as the client RPC handler does not do anything with this value
			if(netId != player.Data.NetId)
			{
				blockRpc = true;
				Anticheat.Flag(player, $"SetColor RPC sent for {player.Data.PlayerName} contains invalid net id, expected {player.Data.NetId}, received {netId}", false);
			}

			if(color >= Palette.ColorNames.Length)
			{
				blockRpc = true;
				Anticheat.Flag(player, $"SetColor RPC sent for {player.Data.PlayerName} contains an invalid color: {color}", false);
			}

			if(blockRpc)
			{
				player.SetColor((byte)CrewmateColor.Red);
			}
		}

		public override bool IsHostOnly()
		{
			return true;
		}
	}
}
