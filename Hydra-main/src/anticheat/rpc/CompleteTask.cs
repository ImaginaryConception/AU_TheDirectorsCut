using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class CompleteTask : RpcCheck
	{
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			uint taskIndex = reader.ReadPackedUInt32();

			
			
			if(ShipStatus.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried completing task {taskIndex} when there was no valid instance of ShipStatus.");
				blockRpc = true;
			}

			if(RoleManager.IsImpostorRole(player.Data.RoleType))
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried completing task {taskIndex} while being an imposter.");
				blockRpc = true;
			}

			
			if(taskIndex + 1 > player.Data.Tasks.Count)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried completing task {taskIndex} when they only have {player.Data.Tasks.Count} tasks.");
				blockRpc = true;
			}
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.CompleteTask;
		}
	}
}