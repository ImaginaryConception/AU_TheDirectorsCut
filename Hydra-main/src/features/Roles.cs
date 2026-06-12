using HarmonyLib;
using UnityEngine;

namespace HydraMenu.features
{
	internal class Roles : MonoBehaviour
	{
		public static bool DisableShapeshiftAnimation { get; set; } = false;
		
		public static bool AllowVentingForCrewmates { get; set; } = true;

		public void Update()
		{
			
			if(PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

			if(SkipSabotageChecks.SabotageAsCrewmate) HudManager.Instance.SabotageButton.gameObject.SetActive(true);
			if(AllowVentingForCrewmates) HudManager.Instance.ImpostorVentButton.gameObject.SetActive(true);
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckShapeshift))]
		class ShapeshiftStart
		{
			static void Prefix(ref bool shouldAnimate)
			{
				if(DisableShapeshiftAnimation) shouldAnimate = false;
			}
		}

		
		
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckRevertShapeshift))]
		class ShapeshiftEnd
		{
			static void Prefix(ref bool shouldAnimate)
			{
				if(DisableShapeshiftAnimation) shouldAnimate = false;
			}
		}

		

		
		
		[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
		public static class SkipSabotageChecks
		{
			public static bool SabotageAsCrewmate { get; set; } = false;
			public static bool SabotageInVents { get; set; } = false;

			static bool Prefix()
			{
				PlayerControl player = PlayerControl.LocalPlayer;

				
				if(!SabotageInVents && player.inVent && !RoleManager.IsImpostorRole(player.Data.RoleType)) return true;

				HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.Sabotage });
				return false;
			}
		}

		
		
		[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
		class SkipVentChecks
		{
			static bool Prefix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
			{
				if(!AllowVentingForCrewmates) return true;

				PlayerControl player = pc.Object;
				if(pc.IsDead) return true;

				couldUse = true;
				__result = Vector2.Distance(player.Collider.bounds.center, __instance.transform.position);

				bool isObstructed = PhysicsHelpers.AnythingBetween(player.Collider, player.Collider.bounds.center, __instance.transform.position, Constants.ShipOnlyMask, false);
				if(__result <= __instance.UsableDistance && !isObstructed) canUse = true;

				return false;
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
		public static class MoveModifier
		{
			public static bool MoveInVents { get; set; } = true;

			static bool Prefix(PlayerControl __instance, ref bool __result)
			{
				if(HudManager.Instance.Chat.IsOpenOrOpening) return true;

				if(__instance.inVent && MoveInVents)
				{
					__result = true;
					return false;
				}

				return true;
			}
		}
	}
}