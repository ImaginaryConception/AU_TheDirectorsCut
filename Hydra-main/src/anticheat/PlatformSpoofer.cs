using HarmonyLib;
using InnerNet;

namespace HydraMenu.anticheat
{
	internal class PlatformSpoofer
	{
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
		class PlatformSpoof
		{
			static void Postfix(PlayerControl __instance)
			{
				if(!Anticheat.Enabled || !Anticheat.CheckSpoofedPlatforms) return;

				ClientData clientData = AmongUsClient.Instance.GetClientFromCharacter(__instance);
				if(clientData == null) return;

				PlatformSpecificData platformData = clientData.PlatformData;

				if(!IsValidPlatform(platformData))
				{
					Anticheat.Flag(__instance, $"{clientData.PlayerName} was detected with spoofed platform information. Platform: {platformData.Platform}, Platform name: {platformData.PlatformName}, XUID: {platformData.XboxPlatformId}, PSID: {platformData.PsnPlatformId}.");
				}
			}
		}

		public static bool IsValidPlatform(PlatformSpecificData platform)
		{
			string platformName = platform.PlatformName;
			ulong xuid = platform.XboxPlatformId;
			ulong psid = platform.PsnPlatformId;

			switch(platform.Platform)
			{
				case Platforms.StandaloneEpicPC:
				case Platforms.StandaloneSteamPC:
				case Platforms.StandaloneMac:
				case Platforms.StandaloneItch:
				case Platforms.IPhone:
				case Platforms.Android:
					if(IsGenericPlatformName(platformName) && xuid == 0 && psid == 0) return true;
					break;

				case Platforms.StandaloneWin10:
					if(IsGenericPlatformName(platformName) && xuid != 0 && psid == 0) return true;
					break;

				case Platforms.Xbox:
					
					
					
					
					if(!IsGenericPlatformName(platformName) && platformName.Length >= 3 && platformName.Length <= 16 && xuid != 0 && psid == 0) return true;
					break;

				case Platforms.Playstation:
					if(!IsGenericPlatformName(platformName) && xuid == 0 && psid != 0) return true;
					break;

				case Platforms.Switch:
					if(!IsGenericPlatformName(platformName) && xuid == 0 && psid == 0) return true;
					break;

				
				case (Platforms)255:
					if(AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return true;
					break;
			}

			
			return false;
		}

		public static bool IsGenericPlatformName(string platformName)
		{
			return platformName == "TESTNAME";
		}
	}
}