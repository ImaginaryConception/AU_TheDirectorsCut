using System;
using System.Collections.Generic;

namespace AU_TheDirectorsCut
{
    public static class ModMessages
    {
        public const string Welcome = "<color=#ff6b6b>THE DIRECTOR'S CUT</color> - Le premier mort devient RÉALISATEUR. Tapez /help pour voir les commandes !";
        public const string WelcomePlain = "THE DIRECTOR'S CUT - Le premier mort devient RÉALISATEUR. Tapez /help pour voir les commandes !";

        public const string Help1 = "<color=#ffd23f>HELP</color>: /help, /welcome, /gg, /players, /join";
        public const string Help1Plain = "HELP: /help, /welcome, /gg, /players, /join";

        public const string Help2 = "<color=#ffd23f>DIRECTOR</color>: /randomcolors, /cut, /darkness, /freeze, /action, /loc, /vote";
        public const string Help2Plain = "DIRECTOR: /randomcolors, /cut, /darkness, /freeze, /action, /loc, /vote";
        public const string Help3 = "<color=#ffd23f>INFO</color>: Utilise /h et la commande pour détails (/hcut, /hloc)";
        public const string Help3Plain = "INFO: Utilise /h et la commande pour détails (/hcut, /hloc)";
        
        public const string HelpLoc = "<color=#ffd23f>/loc ID_joueur ID_zone</color> - Interdit à un joueur d'aller dans une zone. Cooldown:20s";
        public const string HelpLocPlain = "/loc ID_joueur ID_zone - Interdit à un joueur d'aller dans une zone. Cooldown:20s";
        public const string LocList1 = "<color=#ffd23f>Zones (B-I):</color> B=Admin, C=Electrical, D=Storage, E=Security, F=Réacteur, G=UpperEngine";
        public const string LocList1Plain = "Zones (B-I): B=Admin, C=Electrical, D=Storage, E=Security, F=Réacteur, G=UpperEngine";
        public const string LocList2 = "<color=#ffd23f>Zones (J-N):</color> J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons, H=LowerEngine, I=Medbay";
        public const string LocList2Plain = "Zones (J-N): J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons, H=LowerEngine, I=Medbay";
        public const string LocList = LocList1; // Keep for compatibility
        public const string LocListPlain = LocList1Plain;
        
        public const string HelpVote = "<color=#ffd23f>/vote ID_joueur ID_cible</color> - Force un joueur à voter pour une cible spécifique. Cooldown:20s";
        public const string HelpVotePlain = "/vote ID_joueur ID_cible - Force un joueur à voter pour une cible spécifique. Cooldown:20s";
        
        public const string UsageLoc = "Usage: /loc ID_joueur ID_zone (sans ID_zone pour voir la liste des zones)";
        public const string UsageLocPlain = "Usage: /loc ID_joueur ID_zone (sans ID_zone pour voir la liste des zones)";
        
        public const string UsageVote = "Usage: /vote ID_joueur ID_cible";
        public const string UsageVotePlain = "Usage: /vote ID_joueur ID_cible";
        

        
        public const string LocAssigned = "<color=#ffd23f>/loc</color>: Ordre envoyé à {0} !";
        public const string LocAssignedPlain = "/loc: Ordre envoyé à {0} !";
        
        public const string VoteAssigned = "<color=#ffd23f>/vote</color>: Ordre envoyé à {0} !";
        public const string VoteAssignedPlain = "/vote: Ordre envoyé à {0} !";
        public const string OnlyInMeeting = "Cette commande ne peut être utilisée qu'en réunion !";
        public const string OnlyInMeetingPlain = "Cette commande ne peut être utilisée qu'en réunion !";

        public const string HelpRandomColors = "<color=#ffd23f>/randomcolors</color> - Couleurs aléatoires pour TOUS! Cooldown 20s";
        public const string HelpRandomColorsPlain = "/randomcolors - Couleurs aléatoires pour TOUS! Cooldown 20s";

        public const string HelpCut = "<color=#ffd23f>/cut</color> - Alerte sabotage (2s), puis arrêt (5s): bouge = mort! Cooldown 30s";
        public const string HelpCutPlain = "/cut - Alerte sabotage (2s), puis arrêt (5s): bouge = mort! Cooldown 30s";

        public const string HelpDarkness = "<color=#ffd23f>/darkness</color> - NOIR TOTAL pendant 10s! Cooldown 35s";
        public const string HelpDarknessPlain = "/darkness - NOIR TOTAL pendant 10s! Cooldown 35s";

        public const string HelpFreeze = "<color=#ffd23f>/freeze ID</color> - Bloque un joueur 8s ! Cooldown 30s";
        public const string HelpFreezePlain = "/freeze ID - Bloque un joueur 8s ! Cooldown 30s";

        public const string HelpAction = "<color=#ffd23f>/action ID A-D</color> - Donne un script secret à un joueur ! Cooldown 20s";
        public const string HelpActionPlain = "/action ID A-D - Donne un script secret à un joueur ! Cooldown 20s";

        public const string ActionList = "<color=#ffd23f>SCRIPTS</color>: A=NoReport, B=SkipVote, C=NoVents, D=VoteFirst (utilise /helpaction)";
        public const string ActionListPlain = "SCRIPTS:A=NoReport,B=SkipVote,C=NoVents,D=VoteFirst (utilise /helpaction)";

        // /helpaction messages
        public const string HelpActionTitle = "<color=#ffd23f>/helpaction - Liste des scripts détaillée</color>";
        public const string HelpActionTitlePlain = "/helpaction - Liste des scripts détaillée";
        public const string HelpActionA = "<color=#ffd23f>A / NoReport</color>: Tu ne dois pas report de corps ce round !";
        public const string HelpActionAPlain = "A / NoReport: Tu ne dois pas report de corps ce round !";
        public const string HelpActionB = "<color=#ffd23f>B / SkipVote</color>: Tu dois passer ton vote ce round !";
        public const string HelpActionBPlain = "B / SkipVote: Tu dois passer ton vote ce round !";
        public const string HelpActionC = "<color=#ffd23f>C / NoVents</color>: Tu ne dois pas utiliser les vents ce round !";
        public const string HelpActionCPlain = "C / NoVents: Tu ne dois pas utiliser les vents ce round !";
        public const string HelpActionD = "<color=#ffd23f>D / VoteFirst</color>: Tu dois voter en PREMIER ce round !";
        public const string HelpActionDPlain = "D / VoteFirst: Tu dois voter en PREMIER ce round !";

        public const string ActionAssigned = "<color=#ffd23f>SCRIPT</color>: Ordre envoyé à {0} !";
        public const string ActionAssignedPlain = "SCRIPT: Ordre envoyé à {0} !";

        public const string ActionAlreadyActive = "{0} a déjà un script actif !";
        public const string ActionAlreadyActivePlain = "{0} a déjà un script actif !";

        public const string UsageAction = "Usage: /action ID 1/2/3 (sans numéro pour voir la liste)";
        public const string UsageActionPlain = "Usage: /action ID 1/2/3 (sans numéro pour voir la liste)";

        public const string GgNoGame = "<color=#ffd23f>FIN</color> - Aucune partie précédente";
        public const string GgNoGamePlain = "FIN - Aucune partie précédente";

        public const string GgSimple = "<color=#ffd23f>FIN</color> - Partie terminée. GG !";
        public const string GgSimplePlain = "FIN - Partie terminée. GG !";

        public const string GgFormat = "<color=#ffd23f>FIN</color> - Réalisateur : {2} - Vivants : {0} - Éliminés : {1} - GG !";
        public const string GgFormatPlain = "FIN - Réalisateur : {2} - Vivants : {0} - Éliminés : {1} - GG !";

        public const string DirectorSet = "<color=#ffd23f>{0}</color> est le Réalisateur !";
        public const string DirectorSetPlain = "{0} est le Réalisateur !";

        public const string RandomColorsStart = "Couleurs aléatoires pour TOUS !";
        public const string RandomColorsStartPlain = "Couleurs aléatoires pour TOUS !";

        public const string CooldownMsg = "{0} en recharge - {1}s restantes";
        public const string CooldownMsgPlain = "{0} en recharge - {1}s restantes";

        public const string HostOnly = "Hôte seulement !";
        public const string HostOnlyPlain = "Hôte seulement !";

        public const string PlayerNotFound = "Joueur introuvable !";
        public const string PlayerNotFoundPlain = "Joueur introuvable !";

        public const string NotDirector = "{0} : tu n'es pas le Réalisateur !";
        public const string NotDirectorPlain = "{0} : tu n'es pas le Réalisateur !";

        public const string FirstDirector = "<color=#ff6b6b>{0}</color> est le RÉALISATEUR ! (/help)";
        public const string FirstDirectorPlain = "{0} est le RÉALISATEUR ! (/help)";
        public const string Discord = "<color=#ffd23f>Add us on Dxiscord</color> : imaginaryconception or kalinina_sn";
        public const string DiscordPlain = "Add us Dxiscord : imaginaryconception or kalinina_sn";

        public const string SetImpostorSuccess = "<color=#ff6b6b>{0}</color> est désormais Imposteur !";
        public const string SetImpostorSuccessPlain = "{0} est désormais Imposteur !";

        public const string UsageSetImpostor = "Usage : /setimpostor ID";
        public const string UsageSetImpostorPlain = "Usage : /setimpostor ID";

        public const string GameStopped = "<color=#ff6b6b>STOP</color> - Partie arrêtée !";
        public const string GameStoppedPlain = "STOP - Partie arrêtée !";

        public const string NoGameRunning = "<color=#ffd23f>Aucune partie en cours !</color>";
        public const string NoGameRunningPlain = "Aucune partie en cours !";

        public const string KillSuccess = "<color=#ff6b6b>{0}</color> a été éliminé !";
        public const string KillSuccessPlain = "{0} a été éliminé !";

        public const string UsageKill = "Usage : /kill ID";
        public const string UsageKillPlain = "Usage : /kill ID";

        public const string CutStart = "<color=#ffd23f>CUT !</color> Sabotage réacteur (2s) → ARRÊT (5s) : BOUGEZ PAS !";
        public const string CutStartPlain = "CUT ! Sabotage réacteur (2s) → ARRÊT (5s) : BOUGEZ PAS !";

        public const string CutEliminated = "<color=#ff6b6b>{0}</color> a bougé — éliminé !";
        public const string CutEliminatedPlain = "{0} a bougé — éliminé !";

        public const string DarknessStart = "<color=#2d3436>DARKNESS !</color> NOIR TOTAL pendant 10s !";
        public const string DarknessStartPlain = "DARKNESS ! NOIR TOTAL pendant 10s !";

        public const string DarknessEnd = "<color=#ffd23f>LUMIÈRE !</color> Retour à la normale !";
        public const string DarknessEndPlain = "LUMIÈRE ! Retour à la normale !";

        public const string FreezeStart = "<color=#74b9ff>FREEZE !</color> {0} est bloqué 8s !";
        public const string FreezeStartPlain = "FREEZE ! {0} est bloqué 8s !";

        public const string FreezeEnd = "<color=#ffd23f>GO !</color> {0} peut à nouveau bouger !";
        public const string FreezeEndPlain = "GO ! {0} peut à nouveau bouger !";
    }
}
