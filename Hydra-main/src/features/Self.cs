using HarmonyLib;
using AmongUs.Data.Player;

namespace HydraMenu.features
{
	internal class Self
	{
		
		
		
		
		public static bool AlwaysShowTaskAnimations { get; set; } = true;

		

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetScanner))]
		class AlwaysDoScanAnimation
		{
			static bool Prefix(PlayerControl __instance, bool value)
			{
				if(__instance != PlayerControl.LocalPlayer) return true;

				if(AlwaysShowTaskAnimations)
				{
					Network.SendSetScanner(value);
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcPlayAnimation))]
		class AlwaysDoTaskAnimaton
		{
			static bool Prefix(PlayerControl __instance, byte animType)
			{
				if(__instance != PlayerControl.LocalPlayer) return true;

				if(AlwaysShowTaskAnimations)
				{
					Network.SendPlayAnimation(animType);
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		[HarmonyPatch(typeof(PlayerStatsData), nameof(PlayerStatsData.ValidateStat))]
		public static class UpdateStatsFreeplay
		{
			public static bool Enabled { get; set; } = false;

			static void Prefix(PlayerStatsData __instance)
			{
				if(Enabled)
				{
					__instance.isTrackingStats = true;
				}
			}
		}

		[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.TrueSpeed), MethodType.Getter)]
		public static class PlayerSpeedModifier
		{
			public static float Multiplier { get; set; } = 1.0f;

			static void Postfix(ref float __result)
			{
				__result *= Multiplier;
			}
		}

		[HarmonyPatch(typeof(Ladder), nameof(Ladder.SetDestinationCooldown))]
		public static class NoLadderCooldown
		{
			public static bool Enabled { get; set; } = true;
			static void Postfix(Ladder __instance)
			{
				if(Enabled)
				{
					Hydra.Log.LogMessage($"Used ladder");
					__instance.CoolDown = 0.0f;
					__instance.Destination.CoolDown = 0.0f;
				}
			}
		}

		[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Begin))]
		public static class UnlimitedMeetings
		{
			public static bool enabled = true;

			static void Prefix()
			{
				if(enabled) PlayerControl.LocalPlayer.RemainingEmergencies = 999999;
			}
		}
	}
}