using HarmonyLib;

namespace HydraMenu.features
{
    internal class Visuals
    {
        
        
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
        public static class Fullbright
        {
            public static bool Enabled { get; set; } = false;

            static bool Prefix(ref float __result)
            {
                if(!Enabled) return true;

                __result = 1000f;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
        public static class ShowProtections
        {
            public static bool Enabled { get; set; } = true;

            static void Prefix(ref bool visible)
            {
                if(Enabled) visible = true;
            }
        }

        
        
		[HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
		public static class AccurateDisconnectReasons
		{
			public static bool Enabled { get; set; } = true;

			static bool Prefix(string playerName, DisconnectReasons reason)
			{
                if(!Enabled) return true;

				Hydra.Log.LogInfo($"[Disconnect Logger] {playerName} was disconnected with reason {reason}");

				switch(reason) {
                    
                    case DisconnectReasons.ExitGame:
                    case DisconnectReasons.Kicked:
                    case DisconnectReasons.Banned:
                    case DisconnectReasons.Error:
                        return true;

                    case DisconnectReasons.Hacking:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was banned by the Among Us anticheat for hacking.");
						return false;

                    case DisconnectReasons.DuplicateConnectionDetected:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to duplicate login.");
						return false;

                    
                    case DisconnectReasons.ClientTimeout:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to timeout.");
                        return false;

					default:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected due to {reason}.");
						return false;
                }
			}
		}

		[HarmonyPatch(typeof(ShhhBehaviour), nameof(ShhhBehaviour.PlayAnimation))]
		public static class SkipShhhAnimation
		{
			public static bool Enabled { get; set; } = true;

			static bool Prefix()
			{
				if(Enabled)
				{
					HudManager.Instance.shhhEmblem.gameObject.SetActive(false);
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		
		
		
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Visible), MethodType.Setter)]
		public static class ShowGhosts
		{
			public static bool Enabled { get; set; } = true;

			static bool Prefix(PlayerControl __instance)
			{
				if(Enabled && __instance.Data.IsDead)
				{
					__instance.cosmetics.Visible = true;
					return false;
				}
				else
				{
					return true;
				}
			}
		}
	}
}