using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;

namespace AU_TheDirectorsCut.Hydra
{
	internal class Network
	{
		public static void SendSetScanner(bool scanning)
		{
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.NetId,
				(byte)RpcCalls.SetScanner,
				SendOption.Reliable,
				-1
			);

			byte scanCount = ++PlayerControl.LocalPlayer.scannerCount;
			writer.Write(scanning);
			writer.Write(scanCount);

			AmongUsClient.Instance.FinishRpcImmediately(writer);
			PlayerControl.LocalPlayer.SetScanner(scanning, scanCount);
		}

		public static void SendPlayAnimation(byte animation)
		{
			if (ShipStatus.Instance == null) return;

			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.NetId,
				(byte)RpcCalls.PlayAnimation,
				SendOption.None,
				-1
			);

			writer.Write(animation);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
			PlayerControl.LocalPlayer.PlayAnimation(animation);
		}

		public static void SendDataFlag(uint netId, MessageWriter msg, int targetClientId = -1)
		{
			MessageWriter writer = MessageWriter.Get(SendOption.Reliable);

			if (targetClientId == -1)
			{
				writer.StartMessage(InnerNet.Tags.GameData);
				writer.Write(AmongUsClient.Instance.GameId);
			}
			else
			{
				writer.StartMessage(InnerNet.Tags.GameDataTo);
				writer.Write(AmongUsClient.Instance.GameId);
				writer.WritePacked(targetClientId);
			}

			writer.StartMessage((byte)GameDataTypes.DataFlag);
			writer.WritePacked(netId);
			writer.Write(msg, false);
			writer.EndMessage();

			writer.EndMessage();
			AmongUsClient.Instance.SendOrDisconnect(writer);
			writer.Recycle();
		}

		public class BatchedMessage
		{
			public MessageWriter writer;

			public BatchedMessage(int targetClientId = -1)
			{
				writer = MessageWriter.Get(SendOption.Reliable);

				if (targetClientId == -1)
				{
					writer.StartMessage(InnerNet.Tags.GameData);
					writer.Write(AmongUsClient.Instance.GameId);
				}
				else
				{
					writer.StartMessage(InnerNet.Tags.GameDataTo);
					writer.Write(AmongUsClient.Instance.GameId);
					writer.WritePacked(targetClientId);
				}
			}

			public void QueueSetRole(PlayerControl source, RoleTypes role, bool canOverride = false)
			{
				source.StartCoroutine(source.CoSetRole(role, canOverride));

				writer.StartMessage((byte)GameDataTypes.RpcFlag);
				writer.WritePacked(source.NetId);
				writer.Write((byte)RpcCalls.SetRole);
				writer.Write((ushort)role);
				writer.Write(canOverride);
				writer.EndMessage();
			}

			public void QueueShapeshift(PlayerControl source, PlayerControl target, bool shouldAnimate)
			{
				source.Shapeshift(target, shouldAnimate);

				writer.StartMessage((byte)GameDataTypes.RpcFlag);
				writer.WritePacked(source.NetId);
				writer.Write((byte)RpcCalls.Shapeshift);
				writer.WriteNetObject(target);
				writer.Write(shouldAnimate);
				writer.EndMessage();
			}

			public void FinishBatch()
			{
				writer.EndMessage();
				AmongUsClient.Instance.SendOrDisconnect(writer);
				writer.Recycle();
			}
		}
	}
}
