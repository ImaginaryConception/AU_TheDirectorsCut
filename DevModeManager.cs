using HarmonyLib;

namespace AU_TheDirectorsCut
{
    public static class DevModeManager
    {
        
        public static bool devMode = true;

        
        public static bool endGame = false;
    }

    
    
    
    
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    static class DevMode_CheckEndCriteria_P
    {
        static bool Prefix()
        {
            
            if (DevModeManager.devMode && !DevModeManager.endGame && AmongUsClient.Instance?.AmHost == true)
                return false;   
            return true;        
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.EndGame))]
    static class DevMode_EndGame_P
    {
        static bool Prefix()
        {
            if (DevModeManager.devMode && !DevModeManager.endGame && AmongUsClient.Instance?.AmHost == true)
            {
                Plugin.Log?.LogInfo("[DevMode] Blocked EndGame call!");
                return false;
            }
            return true;
        }
    }
}