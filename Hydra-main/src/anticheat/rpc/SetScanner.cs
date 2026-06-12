using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class SetScanner : RpcCheck
	{
		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			bool scanning = reader.ReadBoolean();
			

			
			
			
			if(ShipStatus.Instance == null && scanning)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC while the map has not spawned in yet.");
				blockRpc = true;
			}

			
			
			if(RoleManager.IsImpostorRole(player.Data.RoleType) && scanning)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC when they are an imposter {scanning}.");
				blockRpc = true;
			}

			if(!GameManager.Instance.LogicOptions.GetVisualTasks())
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC while visual tasks were disabled.");
				blockRpc = true;
			}

			bool hasMedbayScanTask = false;
			foreach(NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
			{
				if(task.TypeId != (byte)TaskTypes.SubmitScan) continue;

				hasMedbayScanTask = true;
				break;
			}

			
			if(!hasMedbayScanTask && scanning)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC without being assigned the medbay scan task.");
				blockRpc = true;
			}
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.SetScanner;
		}
	}
}