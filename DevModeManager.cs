using HarmonyLib;

namespace AU_TheDirectorsCut
{
    public static class DevModeManager
    {
        // ⚙️ TOGGLE — true = partie INFINIE (test), false = règles normales du jeu.
        public static bool devMode = true;
    }

    // Bloque DÉFINITIVEMENT la fin de partie tant que devMode == true.
    // LogicGameFlowNormal.CheckEndCriteria est exécuté sur l'hôte : renvoyer false
    // saute la logique vanilla → aucune condition de victoire n'est évaluée,
    // même s'il ne reste qu'un seul joueur en vie.
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    static class DevMode_CheckEndCriteria_P
    {
        static bool Prefix()
        {
            // Hôte uniquement (mod host-only). Pas de log : appelé en boucle.
            if (DevModeManager.devMode && AmongUsClient.Instance?.AmHost == true)
                return false;   // devMode ON  → la partie ne peut pas se terminer
            return true;        // devMode OFF → logique normale
        }
    }
}