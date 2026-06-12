using AmongUs.GameOptions;
using Hazel;
using HydraMenu.features;

namespace HydraMenu
{
	internal class GameOptions
	{
		
		
		
		public static IGameOptions CreateCloneOptions(IGameOptions options)
		{
			LogicOptions logicOptions = GameManager.Instance.LogicOptions;

			byte[] byteArray = logicOptions.gameOptionsFactory.ToBytes(options, AprilFoolsMode.IsAprilFoolsModeToggledOn);
			return logicOptions.gameOptionsFactory.FromBytes(byteArray);
		}

		
		public static void SendGameOptionsToClient(IGameOptions options, int targetClientId)
		{
			
			
			if(AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay && targetClientId == PlayerControl.LocalPlayer.OwnerId)
			{
				GameManager.Instance.LogicOptions.SetGameOptions(options);
				return;
			}

			
			
			if(Protections.BypassShapeshiftRatelimits.Enabled) options.SetFloat(FloatOptionNames.ShapeshifterCooldown, 0.0f);

			MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
			writer.StartMessage((byte)FindLogicOptionsIndex());
			writer.WriteBytesAndSize(GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, AprilFoolsMode.IsAprilFoolsModeToggledOn));
			writer.EndMessage();

			Network.SendDataFlag(GameManager.Instance.NetId, writer, targetClientId);
		}

		private static int FindLogicOptionsIndex()
		{
			int logicIndex = -1;
			for(int i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
			{
				GameLogicComponent component = GameManager.Instance.LogicComponents[i];

				Hydra.Log.LogMessage($"Found component {component.GetType()} at index {i}");
				if(component.GetType() != typeof(LogicOptions)) continue;

				logicIndex = i;
				break;
			}

			Hydra.Log.LogMessage($"Found LogicOptions at index {logicIndex}");
			return logicIndex;
		}
	}
}
