using HarmonyLib;
using InnerNet;

namespace HydraMenu.features
{
	internal class Immortality
	{
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		private static readonly int CUSTOM_VENT_ID = 50;

		private static bool _enabled = false;

		public static bool Enabled
		{
			get
			{
				return _enabled;
			}
			set
			{
				if(value == _enabled) return;

				if(PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.inVent)
				{
					if(value)
					{
						Hydra.Log.LogInfo("Immortality was enabled, sending a VentilationSystem update with operation Enter");
						VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
					}
					else
					{
						Hydra.Log.LogInfo("Immortality was disabled, sending a VentilationSystem update with operation Exit");
						VentilationSystem.Update(VentilationSystem.Operation.Exit, CUSTOM_VENT_ID);
					}
				}

				_enabled = value;
			}
		}

		[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
		class BlockSendingUpdates
		{
			static bool Prefix(VentilationSystem.Operation op, int ventId)
			{
				if(ventId != CUSTOM_VENT_ID && Enabled && (op == VentilationSystem.Operation.Enter || op == VentilationSystem.Operation.Exit || op == VentilationSystem.Operation.Move))
				{
					
					

					Hydra.Log.LogInfo($"Our client sent VentilationSystem operation {op} for vent {ventId}, cancelling..");
					return false;
				}

				return true;
			}
		}

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
		class OnShipStatusCreate
		{
			static void Postfix()
			{
				if(!Enabled) return;

				Hydra.Log.LogMessage($"A new instance of ShipStatus has spawned, sending the immortality RPC");
				VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
		class OnMurder
		{
			static void Postfix(PlayerControl __instance, PlayerControl target)
			{
				if(Enabled && target == PlayerControl.LocalPlayer)
				{
					Hydra.notifications.Send("Immortality", $"{__instance.Data.PlayerName} attempted to kill you!", 5);
				}
			}
		}

		[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
		class OnMeetingEnd
		{
			static void Postfix()
			{
				if(!Enabled || PlayerControl.LocalPlayer.Data.IsDead) return;

				Hydra.Log.LogInfo("Meeting has ended, resending Immortality RPC to retain immortal status");
				VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
			}
		}
	}
}