using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Collections.Generic;
using InnerNet;
using UnityEngine.AddressableAssets;

namespace HydraMenu.features
{
	internal class Host
	{
		private static bool isSkeldFlipped = false;
		public static bool FlippedSkeld
		{
			get { return isSkeldFlipped; }
			set
			{
				if(AmongUsClient.Instance == null || isSkeldFlipped == value) return;

				
				
				
				
				AssetReference temp = AmongUsClient.Instance.ShipPrefabs[3];
				AmongUsClient.Instance.ShipPrefabs[3] = AmongUsClient.Instance.ShipPrefabs[0];
				AmongUsClient.Instance.ShipPrefabs[0] = temp;

				isSkeldFlipped = value;
			}
		}

		
		
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
		public static class DisableMeetings
		{
			public static bool Enabled { get; set; } = false;

			static bool Prefix()
			{
				return !Enabled;
			}
		}

		[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
		public static class DisableSabotages
		{
			public static bool Enabled { get; set; } = false;

			static bool Prefix()
			{
				return !Enabled;
			}
		}

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
		public static class DisableCloseDoors
		{
			public static bool Enabled { get; set; } = false;

			static bool Prefix()
			{
				return !Enabled;
			}
		}

		

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetLevel))]
		public static class BlockLowLevels
		{
			public static bool Enabled { get; set; } = false;
			public static uint MinLevel { get; set; } = 20;

			static void Prefix(PlayerControl __instance, uint level)
			{
				if(!Enabled || !AmongUsClient.Instance.AmHost || __instance == PlayerControl.LocalPlayer || level > MinLevel) return;

				Hydra.notifications.Send("Block Low Levels", $"{__instance.Data.PlayerName} is level {level}, which is below the level threshold. They will be kicked from the game.");
				AmongUsClient.Instance.KickPlayer(__instance.OwnerId, false);
			}
		}

		[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
		public static class BanMidGame
		{
			public static bool Enabled { get; set; } = true;

			static bool Prefix(InnerNetClient __instance, ref bool __result)
			{
				if(!Enabled) return true;

				__result = __instance.AmHost;
				return false;
			}
		}

		
		
		[HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
		public static class DisableCameras
		{
			public static bool Enabled { get; set; } = false;

			static void Postfix(PlayerControl player, MessageReader msgReader)
			{
				if(!Enabled || !AmongUsClient.Instance.AmHost || player.OwnerId == AmongUsClient.Instance.HostId) return;

				
				if(Sabotage.IsSabotageActive(SystemTypes.Comms))
				{
					
					
					
					Hydra.Log.LogMessage($"{player.Data.name} updated security cameras, we do not need to do anything as the Comms sabotage is already active");
					return;
				}

				Hydra.Log.LogMessage($"{player.Data.PlayerName} updated security cameras, sending Comms system update");

				msgReader.Position--;
				
				byte operation = msgReader.ReadByte();

				MessageWriter systemUpdate = MessageWriter.Get(SendOption.Reliable);
				systemUpdate.StartMessage((byte)SystemTypes.Comms);
				
				systemUpdate.Write(operation == 1);
				systemUpdate.EndMessage();

				Network.SendDataFlag(ShipStatus.Instance.NetId, systemUpdate, player.OwnerId);
			}
		}

		[HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
		public static class DisableGameEnd
		{
			public static bool Enabled { get; set; } = false;

			static bool Prefix()
			{
				return !Enabled;
			}
		}

		[HarmonyPatch(typeof(LogicRoleSelectionNormal), nameof(LogicRoleSelectionNormal.AssignRolesFromList))]
		public static class AlwaysImposter
		{
			public static bool Enabled { get; set; } = false;
			public static RoleTypes assignedRole = RoleTypes.Viper;

			
			static void Prefix(ref List<NetworkedPlayerInfo> players, ref List<RoleTypes> roleList, ref int rolesAssigned)
			{
				if(!Enabled || !AmongUsClient.Instance.AmHost) return;

				Hydra.Log.LogInfo($"Attempting to assign ourselves the {assignedRole} role");

				
				Il2CppSystem.Predicate<NetworkedPlayerInfo> predicate = (Il2CppSystem.Predicate<NetworkedPlayerInfo>)(player => player == PlayerControl.LocalPlayer.Data);
				int playerIndex = players.FindIndex(predicate);

				
				
				if(playerIndex == -1)
				{
					Hydra.Log.LogInfo("Our NetworkedPlayerInfo does not exist in this list, skipping");
					return;
				}

				Hydra.Log.LogInfo($"Found our NetworkedPlayerInfo in the players list at index {playerIndex}, removing from the list");
				players.RemoveAt(playerIndex);

				Il2CppSystem.Predicate<RoleTypes> predicate2 = (Il2CppSystem.Predicate<RoleTypes>)(roleType => roleType == assignedRole);
				int roleIndex = roleList.FindIndex(predicate2);

				Hydra.Log.LogMessage($"Player index is {roleIndex}");

				
				
				if(roleIndex != -1)
				{
					Hydra.Log.LogInfo($"Found an instance of our role in the roles list at index {roleIndex}, removing from the list");
					roleList.RemoveAt(roleIndex);
				}

				PlayerControl.LocalPlayer.RpcSetRole(assignedRole);
				rolesAssigned++;

				Hydra.Log.LogInfo($"Assigned ourself the {assignedRole} role!");
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
		public static class NoKillCooldown
		{
			public static bool Enabled { get; set; } = false;

			static void Prefix(PlayerControl __instance, ref float time)
			{
				if(!Enabled || __instance != PlayerControl.LocalPlayer) return;

				time = 0;
			}
		}
	}
}