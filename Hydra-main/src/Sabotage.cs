using System.Collections.Generic;

namespace HydraMenu
{
	internal class Sabotage
	{
		
		public static bool UpdateSystemsDirectly { get; set; } = true;

		public static Dictionary<string, SystemTypes> skeldSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Reactor },
			{ "Oxygen", SystemTypes.LifeSupp },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static Dictionary<string, SystemTypes> skeldDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Cafeteria", SystemTypes.Cafeteria },
			{ "Storage", SystemTypes.Storage },
			{ "Medbay", SystemTypes.MedBay },
			{ "Security", SystemTypes.Security },
			{ "Upper Engine", SystemTypes.UpperEngine },
			{ "Lower Engine", SystemTypes.LowerEngine },
			{ "Electrical", SystemTypes.Electrical }
		};

		public static Dictionary<string, SystemTypes> miraSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Reactor },
			{ "Oxygen", SystemTypes.LifeSupp },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static Dictionary<string, SystemTypes> polusSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Laboratory },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static Dictionary<string, SystemTypes> polusDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Office", SystemTypes.Office },
			{ "Laboratory", SystemTypes.Laboratory },
			{ "Electrical", SystemTypes.Electrical },
			{ "Oxygen", SystemTypes.LifeSupp },
			{ "Communications", SystemTypes.Comms },
			{ "Weapons", SystemTypes.Weapons },
			{ "Storage", SystemTypes.Storage }
		};

		public static Dictionary<string, SystemTypes> airshipSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.HeliSabotage },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static Dictionary<string, SystemTypes> airshipDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Brig", SystemTypes.Brig },
			{ "Records", SystemTypes.Records },
			{ "Communications", SystemTypes.Comms },
			{ "Main Hall", SystemTypes.MainHall },
			{ "Kitchen", SystemTypes.Kitchen },
			{ "Medical", SystemTypes.Medical }
		};

		public static Dictionary<string, SystemTypes> fungleSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Reactor },
			{ "Communications", SystemTypes.Comms },
			{ "Mushroom Mixup", SystemTypes.MushroomMixupSabotage }
		};

		public static Dictionary<string, SystemTypes> GetSabotages()
		{
			MapNames map = Utilities.GetCurrentMap();
			switch(map)
			{
				case MapNames.Skeld:
				case MapNames.Dleks:
					return skeldSabotages;

				case MapNames.MiraHQ:
					return miraSabotages;

				case MapNames.Polus:
					return polusSabotages;

				case MapNames.Airship:
					return airshipSabotages;

				case MapNames.Fungle:
					return fungleSabotages;

				
				default:
					return skeldSabotages;
			}
		}

		public static Dictionary<string, SystemTypes> GetDoors()
		{
			MapNames map = Utilities.GetCurrentMap();
			switch(map)
			{
				case MapNames.Skeld:
				case MapNames.Dleks:
					return skeldDoors;

				
				case MapNames.MiraHQ:
					return [];

				case MapNames.Polus:
					return polusDoors;

				case MapNames.Airship:
					return airshipDoors;

				
				default:
					return skeldDoors;
			}
		}

		
		
		public static bool CanUnlockDoors()
		{
			MapNames map = Utilities.GetCurrentMap();
			return AmongUsClient.Instance.AmHost || map == MapNames.Polus || map == MapNames.Airship || map == MapNames.Fungle;
		}

		public static void SabotageSystem(SystemTypes system)
		{
			if(!UpdateSystemsDirectly)
			{
				ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, (byte)system);
				return;
			}

			switch(system)
			{
				case SystemTypes.Reactor:
				case SystemTypes.Laboratory:
				case SystemTypes.HeliSabotage:
				case SystemTypes.LifeSupp:
				case SystemTypes.Comms:
					ShipStatus.Instance.RpcUpdateSystem(system, 128);
					break;

				
				
				case SystemTypes.Electrical:
					byte amount = 4;

					for(byte i = 0; i < 5; i++)
					{
						if(BoolRange.Next(0.5f))
						{
							amount |= (byte)(1 << i);
						}
					}

					ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(amount | 128));
					break;

				case SystemTypes.MushroomMixupSabotage:
					ShipStatus.Instance.RpcUpdateSystem(system, 1);
					break;
			}
		}

		public static void FixSabotage(SystemTypes system)
		{
			switch(system)
			{
				
				
				case SystemTypes.Reactor:
				case SystemTypes.Laboratory:
				case SystemTypes.LifeSupp:
					ShipStatus.Instance.RpcUpdateSystem(system, 16);
					break;

				
				case SystemTypes.Comms:
				case SystemTypes.HeliSabotage:
					ShipStatus.Instance.RpcUpdateSystem(system, 16);
					ShipStatus.Instance.RpcUpdateSystem(system, 17);
					break;

				case SystemTypes.Electrical:
					SwitchSystem switches = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();

					
					int amount = switches.ActualSwitches ^ switches.ExpectedSwitches;

					if(amount == 0)
					{
						Hydra.Log.LogInfo($"Attempted to fix lights, XOR operation is 0 so that means we have nothing to fix");
						break;
					}

					
					
					
					
					ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(amount | 128));
					break;

				case SystemTypes.MushroomMixupSabotage:
					if(!AmongUsClient.Instance.AmHost)
					{
						Hydra.Log.LogInfo("Attempted to fix Mushroom Mixup, we are not the host so nothing can be done");
						break;
					}

					MushroomMixupSabotageSystem mixupSystem = ShipStatus.Instance.Systems[SystemTypes.MushroomMixupSabotage].Cast<MushroomMixupSabotageSystem>();

					if(!mixupSystem.IsActive)
					{
						Hydra.Log.LogInfo("Attempted to fix Mushroom Mixup, the sabotage is not enabled so we have nothing to fix");
						break;
					}

					Hydra.Log.LogInfo("Attempted to fix Mushroom Mixup, we are the host so it can be fixed");

					mixupSystem.currentSecondsUntilHeal = 0.1f;
					mixupSystem.IsDirty = true;
					break;
			}
		}

		public static bool IsSabotageActive(SystemTypes system)
		{
			ShipStatus.Instance.Systems.TryGetValue(system, out ISystemType systemType);
			if(systemType == null) return false;

			IActivatable activableSystem = systemType.TryCast<IActivatable>();
			if(activableSystem == null)
			{
				Hydra.Log.LogError($"All sabotage types should extend from IActivatable, but yet {system} doesn't");
				return false;
			}

			return activableSystem.IsActive;
		}

		public static void LockDoor(SystemTypes door)
		{
			ShipStatus.Instance.RpcCloseDoorsOfType(door);
		}

		public static void UnlockDoor(byte id)
		{
			if(AmongUsClient.Instance.AmHost)
			{
				MapNames currentMap = Utilities.GetCurrentMap();

				
				for(byte i = 0; i < ShipStatus.Instance.AllDoors.Count; i++)
				{
					OpenableDoor door = ShipStatus.Instance.AllDoors[i];
					if(door.Id != id) continue;
					door.SetDoorway(true);

					if(currentMap == MapNames.Skeld)
					{
						AutoDoorsSystemType doorSystem = ShipStatus.Instance.Systems[SystemTypes.Doors].Cast<AutoDoorsSystemType>();
						doorSystem.dirtyBits |= 1U << i;
					}
					else
					{
						DoorsSystemType doorSystem = ShipStatus.Instance.Systems[SystemTypes.Doors].Cast<DoorsSystemType>();
						doorSystem.IsDirty = true;
					}
				}
				return;
			}

			ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(id | 64));
		}

		public static void SabotageAll()
		{
			Dictionary<string, SystemTypes> sabotages = GetSabotages();
			foreach(SystemTypes system in sabotages.Values)
			{
				SabotageSystem(system);
			}
		}

		public static void FixAllSabotages()
		{
			Dictionary<string, SystemTypes> sabotages = GetSabotages();
			foreach(SystemTypes system in sabotages.Values)
			{
				FixSabotage(system);
			}
		}

		public static void LockAll()
		{
			Dictionary<string, SystemTypes> doors = GetDoors();
			foreach(SystemTypes door in doors.Values)
			{
				LockDoor(door);
			}
		}

		public static void UnlockAll()
		{
			
			foreach(OpenableDoor door in ShipStatus.Instance.AllDoors)
			{
				UnlockDoor((byte)door.Id);
			}
		}
	}
}