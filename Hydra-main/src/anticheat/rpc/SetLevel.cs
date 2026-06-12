using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class SetLevel : RpcCheck
	{
		public readonly uint MAX_PLAYER_LEVEL = 10000;

		
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			uint level = reader.ReadPackedUInt32();

			
			
			if(level > MAX_PLAYER_LEVEL)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent SetLevel RPC with a level that is too high ({level}).");
				blockRpc = true;

				player.SetLevel(MAX_PLAYER_LEVEL);
			}

			
			if(ShipStatus.Instance)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent SetLevel RPC when the game has already started.");
			}
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.SetLevel;
		}
	}
}