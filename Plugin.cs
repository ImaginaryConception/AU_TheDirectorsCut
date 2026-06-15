using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;

namespace AU_TheDirectorsCut
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource? Log;
        private Harmony? harmony;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo($"[{PluginInfo.PLUGIN_NAME}] Loading...");

            ModConfig.Init(Config);

            harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            DirectorCore.Initialize();
            NetworkManager.Initialize();

            // Panneau Admin hôte (IMGUI) — ouvert avec la touche Suppr (Delete)
            AddComponent<AdminUI>();

            Log.LogInfo($"[{PluginInfo.PLUGIN_NAME}] Loaded!");
        }

        public override bool Unload()
        {
            harmony?.UnpatchSelf();
            return base.Unload();
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.anish.au.thedirectorscut";
        public const string PLUGIN_NAME = "AU_TheDirectorsCut";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
