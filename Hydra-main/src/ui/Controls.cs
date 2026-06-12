using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui
{
	internal class Controls
	{
		
		
		
		
		public static readonly List<RoleTypes> RolesList = new List<RoleTypes>()
		{
			RoleTypes.Crewmate,
			RoleTypes.Impostor,
			RoleTypes.Scientist,
			RoleTypes.Engineer,
			RoleTypes.GuardianAngel,
			RoleTypes.Shapeshifter,
			RoleTypes.Noisemaker,
			RoleTypes.Phantom,
			RoleTypes.Tracker,
			RoleTypes.Detective,
			RoleTypes.Viper,
			RoleTypes.CrewmateGhost,
			RoleTypes.ImpostorGhost
		};

		public enum PlayerColors
		{
			Red,
			Blue,
			Green,
			Pink,
			Orange,
			Yellow,
			Black,
			White,
			Purple,
			Brown,
			Cyan,
			Lime,
			Maroon,
			Rose,
			Banana,
			Gray,
			Tan,
			Coral,
			Fortegreen
		}


		public static RoleTypes HorizontalRoleSlider(RoleTypes currentRole)
		{
			int currentValue = RolesList.IndexOf(currentRole);

			byte newValue = (byte)GUILayout.HorizontalSlider(currentValue, 0, RolesList.Count - 1);

			return RolesList[newValue];
		}

		public static PlayerColors HorizontalColorSlider(PlayerColors currentColor)
		{
			return (PlayerColors)GUILayout.HorizontalSlider((int)currentColor, 0, Palette.ColorNames.Length);
		}


		public static PlayerControl PlayerSpecificToggle(string label, PlayerControl selectedPlayer, PlayerControl currentPlayer)
		{
			GUIStyle toggle = new GUIStyle(GUI.skin.toggle);
			
			bool isCurrentSelection = selectedPlayer != null && selectedPlayer == currentPlayer;

			if(isCurrentSelection)
			{
				toggle.normal = toggle.onNormal;
				toggle.active = toggle.onActive;
				toggle.hover = toggle.onHover;
			}

			
			
			
			if(!GUILayout.Button(label, toggle)) return currentPlayer;

			return isCurrentSelection ? null : selectedPlayer;
		}

		public static void DrawCrewmateColorBox(Rect rect, NetworkedPlayerInfo player)
		{
			string colorName = Utilities.GetPlayerColor(player);
			GUI.Box(rect, "", Styles.CreateCrewmateColorBox(colorName, colorName != "Fortegreen" ? player.Color : Color.black));
		}
	}
}