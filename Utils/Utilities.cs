using AmongUs.GameOptions;
using System.Collections.Generic;

namespace AU_TheDirectorsCut.Utils
{
    internal class Utilities
    {
        public static PlayerControl GetRandomPlayer(bool excludeHost = false, bool excludeDead = false, bool excludeImposters = false, bool excludeSelf = true)
        {
            Il2CppSystem.Collections.Generic.List<PlayerControl> allPlayers = PlayerControl.AllPlayerControls;
            List<PlayerControl> validPlayers = new List<PlayerControl>();

            foreach (PlayerControl player in allPlayers)
            {
                if (
                    (excludeSelf && AmongUsClient.Instance.ClientId == player.OwnerId) ||
                    (excludeHost && AmongUsClient.Instance.HostId == player.OwnerId) ||
                    (excludeDead && player.Data.IsDead) ||
                    (excludeImposters && player.Data.Role.CanUseKillButton)
                ) continue;

                validPlayers.Add(player);
            }

            System.Random rnd = new System.Random();
            return validPlayers[rnd.Next(validPlayers.Count)];
        }

        public static MapNames GetCurrentMap()
        {
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                return (MapNames)AmongUsClient.Instance.TutorialMapId;
            }
            else
            {
                return (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
            }
        }

        public static bool IsAnticheatPresent()
        {
            if (Constants.IsVersionModded() || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;
            return PlayerControl.LocalPlayer.Data.OwnerId == -4;
        }

        public static string GetPlayerColor(NetworkedPlayerInfo player)
        {
            int colorId = player.DefaultOutfit.ColorId;
            if (colorId < 0 || colorId >= Palette.ColorNames.Length)
            {
                return "Fortegreen";
            }
            return player.GetPlayerColorString();
        }
    }
}
