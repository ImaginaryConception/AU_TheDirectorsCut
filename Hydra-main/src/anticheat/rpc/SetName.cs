using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class SetName : RpcCheck
	{
		
		public readonly int MAX_NAME_LENGTH = 12;

		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			
			if(!Utilities.IsAnticheatPresent()) return;

			uint netId = reader.ReadUInt32();
			string requestedName = reader.ReadString();

			if(netId != GetExpectedNetId(player))
			{
				blockRpc = true;
				Anticheat.Flag(player, $"SetName RPC sent for {requestedName} includes an invalid net id, received {netId}, expected {GetExpectedNetId(player)}.");
			}

			if(requestedName.Length > MAX_NAME_LENGTH)
			{
				blockRpc = true;
				Anticheat.Flag(player, $"{requestedName} tried setting their name to something too long ({requestedName.Length}).");
			}

			if(requestedName.Contains('<'))
			{
				blockRpc = true;
				Anticheat.Flag(player, $"{requestedName} requested a name with invalid characters.");
			}
		}

		private uint GetExpectedNetId(PlayerControl player)
		{
			
			
			
			return Utilities.IsAnticheatPresent() ? player.NetId : player.Data.NetId;
		}

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.SetName;
		}

		
		
		public override bool IsHostOnly()
		{
			return !Utilities.IsAnticheatPresent();
		}
	}
}
