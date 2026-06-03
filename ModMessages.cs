using System;
using System.Collections.Generic;

namespace AU_TheDirectorsCut
{
    public static class ModMessages
    {
        // ================================================
        // 📝 MESSAGES DU MOD - MODIFIEZ ICI !
        // ================================================

        // ────────────────────────────────────────────────
        // MESSAGE DE BIENVENUE
        // ────────────────────────────────────────────────
        public const string Welcome = "<color=#ff6b6b>THE DIRECTOR'S CUT</color> - Le premier mort devient RÉALISATEUR. Tapez /help pour voir les commandes !";
        public const string WelcomePlain = "THE DIRECTOR'S CUT - Le premier mort devient RÉALISATEUR. Tapez /help pour voir les commandes !";

        // ────────────────────────────────────────────────
        // MESSAGES D'AIDE (CHAQUE <120 CARACTÈRES !)
        // ────────────────────────────────────────────────
        
        public const string Help1 = "<color=#ffd23f>HELP 1/3</color>: /help, /welcome, /gg, /players";
        public const string Help1Plain = "HELP 1/3: /help, /welcome, /gg, /players";
        
        public const string Help2 = "<color=#88ccff>HELP 2/3</color>: DIRECTIVES → /cut, /swap ID1 ID2, /blind ID, /darkness";
        public const string Help2Plain = "HELP 2/3: DIRECTIVES → /cut, /swap ID1 ID2, /blind ID, /darkness";
        
        public const string Help3 = "<color=#00ff88>HELP 3/3</color>: /freeze ID, /spin ID, /randomcolors, /shuffle, /teleportall ID - /h[cmd] pour détails";
        public const string Help3Plain = "HELP 3/3: /freeze ID, /spin ID, /randomcolors, /shuffle, /teleportall ID - /h[cmd] pour détails";

        // Aides détaillées (/hcut, /hswap, etc.)
        public const string HelpCut = "<color=#ffd23f>/cut</color> - Immobilisez-vous! Durée 5s - Cooldown 30s";
        public const string HelpCutPlain = "/cut - Immobilisez-vous! Durée 5s - Cooldown 30s";

        public const string HelpSwap = "<color=#ffd23f>/swap</color> - Échange positions de 2 joueurs. IDs avec /players! Cooldown 15s";
        public const string HelpSwapPlain = "/swap - Échange positions de deux joueurs. IDs avec /players! Cooldown 15s";

        public const string HelpBlind = "<color=#ffd23f>/blind</color> - Réduit vision d'un joueur pendant 8s! Cooldown 25s";
        public const string HelpBlindPlain = "/blind - Réduit vision d'un joueur pendant 8s! Cooldown 25s";

        public const string HelpDarkness = "<color=#ffd23f>/darkness</color> - Obscurité TOTALE 10s! Cooldown 35s";
        public const string HelpDarknessPlain = "/darkness - Obscurité TOTALE 10s! Cooldown 35s";

        public const string HelpFreeze = "<color=#ffd23f>/freeze</color> - Immobilise un joueur 8s! Cooldown 30s";
        public const string HelpFreezePlain = "/freeze - Immobilise un joueur 8s! Cooldown 30s";

        public const string HelpSpin = "<color=#ffd23f>/spin</color> - Fait tourner un joueur 5s! Cooldown 20s";
        public const string HelpSpinPlain = "/spin - Fait tourner un joueur 5s! Cooldown 20s";

        public const string HelpRandomColors = "<color=#ffd23f>/randomcolors</color> - Couleurs aléatoires pour TOUS! Cooldown 20s";
        public const string HelpRandomColorsPlain = "/randomcolors - Couleurs aléatoires pour TOUS! Cooldown 20s";

        public const string HelpShuffle = "<color=#ffd23f>/shuffle</color> - Mélange toutes les positions! Cooldown 25s";
        public const string HelpShufflePlain = "/shuffle - Mélange toutes les positions! Cooldown 25s";

        public const string HelpTeleportAll = "<color=#ffd23f>/teleportall</color> - Téléporte TOUS vers un joueur! Cooldown 20s";
        public const string HelpTeleportAllPlain = "/teleportall - Téléporte TOUS vers un joueur! Cooldown 20s";

        // ────────────────────────────────────────────────
        // MESSAGES DE FIN DE PARTIE (GG)
        // ────────────────────────────────────────────────
        public const string GgNoGame = "<color=#ffd23f>FIN</color> - Aucune partie précédente";
        public const string GgNoGamePlain = "FIN - Aucune partie précédente";

        public const string GgSimple = "<color=#ffd23f>FIN</color> - Partie terminée. GG !";
        public const string GgSimplePlain = "FIN - Partie terminée. GG !";

        public const string GgFormat = "<color=#ffd23f>FIN</color> - Vivants : {0} - Éliminés : {1} - GG !";
        public const string GgFormatPlain = "FIN - Vivants : {0} - Éliminés : {1} - GG !";

        // ────────────────────────────────────────────────
        // AUTRES MESSAGES
        // ────────────────────────────────────────────────
        public const string DirectorSet = "<color=#ffd23f>{0}</color> est le Réalisateur !";
        public const string DirectorSetPlain = "{0} est le Réalisateur !";

        public const string CutStart = "<color=#ffd23f>CUT !</color> Immobilisez-vous dans 2s !";
        public const string CutStartPlain = "CUT ! Immobilisez-vous dans 2s !";

        public const string PlayerCaught = "<color=#ff6b6b>{0}</color> a bougé - éliminé !";
        public const string PlayerCaughtPlain = "{0} a bougé - éliminé !";

        public const string Sun = "<color=#00ff88>SOLEIL !</color> Vous pouvez bouger !";
        public const string SunPlain = "SOLEIL ! Vous pouvez bouger !";

        public const string SwapSuccess = "Échange : {0} ↔ {1} !";
        public const string SwapSuccessPlain = "Échange : {0} ↔ {1} !";

        public const string BlindSuccess = "Vision réduite pour {0} (8s) !";
        public const string BlindSuccessPlain = "Vision réduite pour {0} (8s) !";

        public const string FreezeSuccess = "{0} est gelé (8s) !";
        public const string FreezeSuccessPlain = "{0} est gelé (8s) !";

        public const string FreezeEnd = "{0} peut rebouger !";
        public const string FreezeEndPlain = "{0} peut rebouger !";

        public const string SpinSuccess = "{0} tourne en rond (5s) !";
        public const string SpinSuccessPlain = "{0} tourne en rond (5s) !";

        public const string RandomColorsStart = "Couleurs aléatoires pour TOUS !";
        public const string RandomColorsStartPlain = "Couleurs aléatoires pour TOUS !";

        public const string ShuffleStart = "Mélange des positions !";
        public const string ShuffleStartPlain = "Mélange des positions !";

        public const string TeleportAllStart = "TOUS téléportés vers {0} !";
        public const string TeleportAllStartPlain = "TOUS téléportés vers {0} !";

        public const string CooldownMsg = "{0} en recharge - {1}s restantes";
        public const string CooldownMsgPlain = "{0} en recharge - {1}s restantes";

        public const string HostOnly = "Hôte seulement !";
        public const string HostOnlyPlain = "Hôte seulement !";

        public const string PlayerNotFound = "Joueur introuvable !";
        public const string PlayerNotFoundPlain = "Joueur introuvable !";

        public const string UsageSwap = "Usage : /swap [ID1] [ID2]";
        public const string UsageSwapPlain = "Usage : /swap [ID1] [ID2]";

        public const string UsageBlind = "Usage : /blind [ID]";
        public const string UsageBlindPlain = "Usage : /blind [ID]";

        public const string UsageFreeze = "Usage : /freeze [ID]";
        public const string UsageFreezePlain = "Usage : /freeze [ID]";

        public const string UsageSpin = "Usage : /spin [ID]";
        public const string UsageSpinPlain = "Usage : /spin [ID]";

        public const string UsageTeleportAll = "Usage : /teleportall [ID]";
        public const string UsageTeleportAllPlain = "Usage : /teleportall [ID]";

        public const string NotDirector = "{0} : tu n'es pas le Réalisateur !";
        public const string NotDirectorPlain = "{0} : tu n'es pas le Réalisateur !";

        public const string FirstDirector = "<color=#ff6b6b>{0}</color> est le RÉALISATEUR ! (/help)";
        public const string FirstDirectorPlain = "{0} est le RÉALISATEUR ! (/help)";
    }
}
