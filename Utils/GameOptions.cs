using AmongUs.GameOptions;
using Hazel;
using System;

namespace AU_TheDirectorsCut.Utils
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
            try
            {
                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] START - targetClientId: {targetClientId}, self id: {PlayerControl.LocalPlayer.OwnerId}");

                if (targetClientId == PlayerControl.LocalPlayer.OwnerId)
                {
                    Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Applying locally to self");
                    GameManager.Instance.LogicOptions.SetGameOptions(options);
                }

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Getting MessageWriter");
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable);

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Finding LogicOptions index");
                int logicIndex = FindLogicOptionsIndex();
                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Found index: {logicIndex}");

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Starting message with index: {logicIndex}");
                writer.StartMessage((byte)logicIndex);

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Writing options bytes");
                byte[] optionsBytes = GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, AprilFoolsMode.IsAprilFoolsModeToggledOn);
                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Options bytes length: {optionsBytes.Length}");

                writer.WriteBytesAndSize(optionsBytes);

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Ending message");
                writer.EndMessage();

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] Calling Network.SendDataFlag with netId: {GameManager.Instance.NetId}, target: {targetClientId}");
                Network.SendDataFlag(GameManager.Instance.NetId, writer, targetClientId);
                Plugin.Log?.LogInfo($"[Hydra.GameOptions.SendGameOptionsToClient] DONE successfully!");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Hydra.GameOptions.SendGameOptionsToClient] EXCEPTION: {ex}");
            }
        }

        private static int FindLogicOptionsIndex()
        {
            try
            {
                Plugin.Log?.LogInfo($"[Hydra.GameOptions.FindLogicOptionsIndex] START");
                int logicIndex = -1;
                Plugin.Log?.LogInfo($"[Hydra.GameOptions.FindLogicOptionsIndex] LogicComponents count: {GameManager.Instance.LogicComponents.Count}");

                for (int i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
                {
                    GameLogicComponent component = GameManager.Instance.LogicComponents[i];
                    Plugin.Log?.LogInfo($"[Hydra.GameOptions.FindLogicOptionsIndex] Component {i}: Type {component.GetType().Name}");

                    if (component.GetType() == typeof(LogicOptions))
                    {
                        logicIndex = i;
                        Plugin.Log?.LogInfo($"[Hydra.GameOptions.FindLogicOptionsIndex] FOUND LogicOptions at index {i}");
                        break;
                    }
                }

                Plugin.Log?.LogInfo($"[Hydra.GameOptions.FindLogicOptionsIndex] Returning: {logicIndex}");
                return logicIndex;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Hydra.GameOptions.FindLogicOptionsIndex] EXCEPTION: {ex}");
                return -1;
            }
        }
    }
}
