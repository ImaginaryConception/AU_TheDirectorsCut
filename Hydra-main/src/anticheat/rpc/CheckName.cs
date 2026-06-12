using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class CheckName : RpcCheck
	{
		public readonly int MAX_NAME_LENGTH = 10;

		public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
		{
			string requestedName = reader.ReadString();

			

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

		public override RpcCalls GetRpcCall()
		{
			return RpcCalls.CheckName;
		}
	}
}
