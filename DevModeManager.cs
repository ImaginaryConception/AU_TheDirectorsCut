using HarmonyLib;

namespace AU_TheDirectorsCut
{
    public static class DevModeManager
    {
        // ⚙️ TOGGLE — true = partie INFINIE (test), false = règles normales du jeu.
        public static bool devMode = true;

        // ⚙️ TOGGLE INDEPENDANT — true = game peut finir normalement, false = game ne finit jamais
        public static bool endGame = false;
    }

    // Bloque DÉFINITIVEMENT la fin de partie tant que devMode == true ET endGame == false.
    // LogicGameFlowNormal.CheckEndCriteria est exécuté sur l'hôte : renvoyer false
    // saute la logique vanilla → aucune condition de victoire n'est évaluée,
    // même s'il ne reste qu'un seul joueur en vie.
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    static class DevMode_CheckEndCriteria_P
    {
        static bool Prefix()
        {
            // Hôte uniquement (mod host-only). Pas de log : appelé en boucle.
            if (DevModeManager.devMode && !DevModeManager.endGame && AmongUsClient.Instance?.AmHost == true)
                return false;   // devMode ON ET endGame OFF → la partie ne peut pas se terminer
            return true;        // sinon → logique normale
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