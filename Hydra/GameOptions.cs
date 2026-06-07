using AmongUs.GameOptions;
using Hazel;

namespace AU_TheDirectorsCut.Hydra
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
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay && targetClientId == PlayerControl.LocalPlayer.OwnerId)
            {
                GameManager.Instance.LogicOptions.SetGameOptions(options);
                return;
            }

            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
            writer.StartMessage((byte)FindLogicOptionsIndex());
            writer.WriteBytesAndSize(GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, AprilFoolsMode.IsAprilFoolsModeToggledOn));
            writer.EndMessage();

            Network.SendDataFlag(GameManager.Instance.NetId, writer, targetClientId);
        }

        private static int FindLogicOptionsIndex()
        {
            int logicIndex = -1;
            for (int i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                GameLogicComponent component = GameManager.Instance.LogicComponents[i];
                if (component.GetType() == typeof(LogicOptions))
                {
                    logicIndex = i;
                    break;
                }
            }
            return logicIndex;
        }
    }
}
