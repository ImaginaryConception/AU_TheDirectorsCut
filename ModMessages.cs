using System;
using System.Collections.Generic;

namespace AU_TheDirectorsCut
{
    public static class ModMessages
    {
        // ============================================================
        // Palette utilisée : marque #ff6b6b (rouge), titres #ffd23f (or),
        // accent #3B9DFF (bleu), succès #00e676 (vert), sombre #b2bec3.
        // Le rich text TMP (<b>, <color>, \n) est désormais envoyé à TOUS
        // les joueurs : un seul message par commande, formaté, sans découpage.
        // ============================================================

        public const string Welcome = "<b><color=#ff6b6b>THE DIRECTOR'S CUT</color></b>\nLe premier mort devient <b><color=#ffd23f>RÉALISATEUR</color></b>.\nTape <b>/help</b> pour voir toutes les commandes !";
        public const string WelcomePlain = "THE DIRECTOR'S CUT - Le premier mort devient REALISATEUR. Tape /help pour voir les commandes !";

        // ---- /help : un seul message complet ----
        public const string HelpFull =
            "<b><color=#ff6b6b>THE DIRECTOR'S CUT</color></b>\n" +
            "<b><color=#ffd23f>Général</color></b> : /help, /welcome, /gg, /players, /join, /discord\n" +
            "<b><color=#ffd23f>Réalisateur — jeu</color></b> : /cut, /darkness, /freeze A, /randomcolors, /colorblinds\n" +
            "<b><color=#ffd23f>Réalisateur — téléport</color></b> : /shuffle, /swap A B, /teleportall A\n" +
            "<b><color=#ffd23f>Réalisateur — réunion</color></b> : /action A X, /loc A Z, /vote A B\n" +
            "<b><color=#ffd23f>Détails</color></b> : /hcut, /hloc, /haction, /hvote, /hfreeze, /hdarkness";
        public const string HelpFullPlain =
            "THE DIRECTOR'S CUT\n" +
            "General : /help, /welcome, /gg, /players, /join, /discord\n" +
            "Realisateur - jeu : /cut, /darkness, /freeze A, /randomcolors, /colorblinds\n" +
            "Realisateur - teleport : /shuffle, /swap A B, /teleportall A\n" +
            "Realisateur - reunion : /action A X, /loc A Z, /vote A B\n" +
            "Details : /hcut, /hloc, /haction, /hvote, /hfreeze, /hdarkness";

        // Anciennes lignes (conservées pour compatibilité, plus utilisées par /help)
        public const string Help1 = "<b><color=#ffd23f>HELP</color></b> : /help, /welcome, /gg, /players, /join";
        public const string Help1Plain = "HELP: /help, /welcome, /gg, /players, /join";
        public const string Help2 = "<b><color=#ffd23f>DIRECTOR</color></b> : /randomcolors, /cut, /darkness, /freeze, /action, /loc, /vote";
        public const string Help2Plain = "DIRECTOR: /randomcolors, /cut, /darkness, /freeze, /action, /loc, /vote";
        public const string Help3 = "<b><color=#ffd23f>INFO</color></b> : Utilise /h + la commande pour les détails (/hcut, /hloc)";
        public const string Help3Plain = "INFO: Utilise /h et la commande pour details (/hcut, /hloc)";
        public const string Help4 = "<b><color=#ffd23f>TÉLÉPORT</color></b> : /shuffle, /swap A B, /teleportall A, /colorblinds";
        public const string Help4Plain = "TELEPORT: /shuffle, /swap A B, /teleportall A, /colorblinds";

        // ---- /hloc : un seul message ----
        public const string HelpLoc = "<b><color=#ffd23f>/loc LETTRE ZONE</color></b> — Interdit une zone à un joueur. Cooldown 20s";
        public const string HelpLocPlain = "/loc LETTRE ZONE - Interdit une zone a un joueur. Cooldown 20s";
        public const string HelpLocFull =
            "<b><color=#ffd23f>/loc LETTRE ZONE</color></b> — interdit une zone à un joueur ce round <i>(réunion)</i>. Cooldown 20s.\n" +
            "<b>Zones :</b>\n" +
            "<color=#3B9DFF>B</color>=Admin  <color=#3B9DFF>C</color>=Electrical  <color=#3B9DFF>D</color>=Storage  <color=#3B9DFF>E</color>=Security  <color=#3B9DFF>F</color>=Réacteur\n" +
            "<color=#3B9DFF>G</color>=UpperEngine  <color=#3B9DFF>H</color>=LowerEngine  <color=#3B9DFF>I</color>=Medbay  <color=#3B9DFF>J</color>=Communications\n" +
            "<color=#3B9DFF>K</color>=Shields  <color=#3B9DFF>L</color>=O2  <color=#3B9DFF>M</color>=Navigation  <color=#3B9DFF>N</color>=Weapons\n" +
            "Ex : <b>/loc A B</b>";
        public const string HelpLocFullPlain =
            "/loc LETTRE ZONE - interdit une zone a un joueur ce round (reunion). Cooldown 20s.\n" +
            "Zones : B=Admin C=Electrical D=Storage E=Security F=Reacteur G=UpperEngine H=LowerEngine I=Medbay J=Communications K=Shields L=O2 M=Navigation N=Weapons\n" +
            "Ex : /loc A B";

        public const string LocList1 = "<b><color=#ffd23f>Zones (B-G)</color></b> : B=Admin, C=Electrical, D=Storage, E=Security, F=Réacteur, G=UpperEngine";
        public const string LocList1Plain = "Zones (B-G): B=Admin, C=Electrical, D=Storage, E=Security, F=Reacteur, G=UpperEngine";
        public const string LocList2 = "<b><color=#ffd23f>Zones (H-N)</color></b> : H=LowerEngine, I=Medbay, J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons";
        public const string LocList2Plain = "Zones (H-N): H=LowerEngine, I=Medbay, J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons";
        public const string LocList = LocList1;
        public const string LocListPlain = LocList1Plain;

        public const string HelpVote = "<b><color=#ffd23f>/vote LETTRE CIBLE</color></b> — Force un joueur à voter une cible précise <i>(réunion)</i>. Cooldown 20s.\nEx : <b>/vote A B</b>";
        public const string HelpVotePlain = "/vote LETTRE CIBLE - Force un joueur a voter une cible precise (reunion). Cooldown 20s. Ex: /vote A B";

        public const string UsageLoc = "<b>Usage :</b> /loc LETTRE ZONE (ex : /loc A B = interdit la zone B au joueur A)";
        public const string UsageLocPlain = "Usage: /loc LETTRE ZONE (ex: /loc A B)";

        public const string UsageVote = "<b>Usage :</b> /vote LETTRE CIBLE (ex : /vote A B = force A à voter B)";
        public const string UsageVotePlain = "Usage: /vote LETTRE CIBLE (ex: /vote A B)";

        public const string LocAssigned = "<b><color=#00e676>/loc</color></b> — Ordre envoyé à <b>{0}</b> !";
        public const string LocAssignedPlain = "/loc: Ordre envoye a {0} !";

        public const string VoteAssigned = "<b><color=#00e676>/vote</color></b> — Ordre envoyé à <b>{0}</b> !";
        public const string VoteAssignedPlain = "/vote: Ordre envoye a {0} !";

        public const string OnlyInMeeting = "<color=#ff6b6b>Cette commande ne s'utilise qu'en <b>réunion</b> !</color>";
        public const string OnlyInMeetingPlain = "Cette commande ne s'utilise qu'en reunion !";

        public const string HelpRandomColors = "<b><color=#ffd23f>/randomcolors</color></b> — Couleurs aléatoires pour TOUS ! Cooldown 20s";
        public const string HelpRandomColorsPlain = "/randomcolors - Couleurs aleatoires pour TOUS ! Cooldown 20s";

        public const string HelpCut = "<b><color=#ff6b6b>/cut</color></b> — Alerte sabotage (2s) puis <b>arrêt</b> (5s) : <b>tous</b> ceux qui bougent meurent (sauf l'hôte) ! Cooldown 30s";
        public const string HelpCutPlain = "/cut - Alerte sabotage (2s) puis arret (5s): tous ceux qui bougent meurent (sauf hote) ! Cooldown 30s";

        public const string HelpDarkness = "<b><color=#2d3436>/darkness</color></b> — <b>NOIR TOTAL</b> pendant 10s ! Cooldown 35s";
        public const string HelpDarknessPlain = "/darkness - NOIR TOTAL pendant 10s ! Cooldown 35s";

        public const string HelpFreeze = "<b><color=#74b9ff>/freeze LETTRE</color></b> — Bloque un joueur 8s ! Cooldown 30s\nEx : <b>/freeze A</b>";
        public const string HelpFreezePlain = "/freeze LETTRE - Bloque un joueur 8s ! Cooldown 30s. Ex: /freeze A";

        public const string HelpAction = "<b><color=#ffd23f>/action LETTRE SCRIPT</color></b> — Donne un script secret à un joueur ! Cooldown 20s";
        public const string HelpActionPlain = "/action LETTRE SCRIPT - Donne un script secret a un joueur ! Cooldown 20s";

        public const string ActionList = "<b><color=#ffd23f>SCRIPTS</color></b> : <color=#3B9DFF>A</color>=NoReport, <color=#3B9DFF>B</color>=SkipVote, <color=#3B9DFF>C</color>=NoVents, <color=#3B9DFF>D</color>=VoteFirst";
        public const string ActionListPlain = "SCRIPTS: A=NoReport, B=SkipVote, C=NoVents, D=VoteFirst";

        // ---- /haction et /helpaction : un seul message ----
        public const string HelpActionFull =
            "<b><color=#ffd23f>/action LETTRE SCRIPT</color></b> — ordre secret à un joueur <i>(réunion)</i>. Cooldown 20s.\n" +
            "<b><color=#3B9DFF>A</color> / NoReport</b> : ne doit pas signaler de corps.\n" +
            "<b><color=#3B9DFF>B</color> / SkipVote</b> : doit passer son vote.\n" +
            "<b><color=#3B9DFF>C</color> / NoVents</b> : ne doit pas utiliser les vents.\n" +
            "<b><color=#3B9DFF>D</color> / VoteFirst</b> : doit voter en premier.\n" +
            "Ex : <b>/action A B</b>";
        public const string HelpActionFullPlain =
            "/action LETTRE SCRIPT - ordre secret a un joueur (reunion). Cooldown 20s.\n" +
            "A / NoReport : ne doit pas signaler de corps.\n" +
            "B / SkipVote : doit passer son vote.\n" +
            "C / NoVents : ne doit pas utiliser les vents.\n" +
            "D / VoteFirst : doit voter en premier.\n" +
            "Ex : /action A B";

        public const string HelpActionTitle = "<b><color=#ffd23f>/helpaction — Liste détaillée des scripts</color></b>";
        public const string HelpActionTitlePlain = "/helpaction - Liste detaillee des scripts";
        public const string HelpActionA = "<b><color=#3B9DFF>A / NoReport</color></b> : Tu ne dois pas signaler de corps ce round !";
        public const string HelpActionAPlain = "A / NoReport: Tu ne dois pas signaler de corps ce round !";
        public const string HelpActionB = "<b><color=#3B9DFF>B / SkipVote</color></b> : Tu dois passer ton vote ce round !";
        public const string HelpActionBPlain = "B / SkipVote: Tu dois passer ton vote ce round !";
        public const string HelpActionC = "<b><color=#3B9DFF>C / NoVents</color></b> : Tu ne dois pas utiliser les vents ce round !";
        public const string HelpActionCPlain = "C / NoVents: Tu ne dois pas utiliser les vents ce round !";
        public const string HelpActionD = "<b><color=#3B9DFF>D / VoteFirst</color></b> : Tu dois voter en PREMIER ce round !";
        public const string HelpActionDPlain = "D / VoteFirst: Tu dois voter en PREMIER ce round !";

        public const string ActionAssigned = "<b><color=#00e676>SCRIPT</color></b> — Ordre envoyé à <b>{0}</b> !";
        public const string ActionAssignedPlain = "SCRIPT: Ordre envoye a {0} !";

        public const string ActionAlreadyActive = "<color=#ff6b6b><b>{0}</b> a déjà un script actif !</color>";
        public const string ActionAlreadyActivePlain = "{0} a deja un script actif !";

        public const string UsageAction = "<b>Usage :</b> /action LETTRE SCRIPT (ex : /action A B = SkipVote au joueur A)";
        public const string UsageActionPlain = "Usage: /action LETTRE SCRIPT (ex: /action A B)";

        public const string GgNoGame = "<b><color=#ffd23f>FIN</color></b> — Aucune partie précédente";
        public const string GgNoGamePlain = "FIN - Aucune partie precedente";

        public const string GgSimple = "<b><color=#ffd23f>FIN</color></b> — Partie terminée. <b>GG !</b>";
        public const string GgSimplePlain = "FIN - Partie terminee. GG !";

        public const string GgFormat = "<b><color=#ffd23f>FIN DE PARTIE</color></b>\n<b>Réalisateur :</b> {2}\n<b><color=#00e676>Vivants :</color></b> {0}\n<b><color=#ff6b6b>Éliminés :</color></b> {1}\n<b>GG !</b>";
        public const string GgFormatPlain = "FIN - Realisateur : {2} - Vivants : {0} - Elimines : {1} - GG !";

        public const string DirectorSet = "<b><color=#ffd23f>{0}</color></b> est le Réalisateur !";
        public const string DirectorSetPlain = "{0} est le Realisateur !";

        public const string RandomColorsStart = "<b><color=#ffd23f>COULEURS !</color></b> Couleurs aléatoires pour TOUS !";
        public const string RandomColorsStartPlain = "Couleurs aleatoires pour TOUS !";

        public const string CooldownMsg = "<color=#ffd23f><b>{0}</b> en recharge — {1}s restantes</color>";
        public const string CooldownMsgPlain = "{0} en recharge - {1}s restantes";

        public const string HostOnly = "<color=#ff6b6b><b>Hôte seulement !</b></color>";
        public const string HostOnlyPlain = "Hote seulement !";

        public const string PlayerNotFound = "<color=#ff6b6b>Joueur introuvable !</color>";
        public const string PlayerNotFoundPlain = "Joueur introuvable !";

        public const string NotDirector = "<color=#ff6b6b><b>{0}</b> : tu n'es pas le Réalisateur !</color>";
        public const string NotDirectorPlain = "{0} : tu n'es pas le Realisateur !";

        public const string FirstDirector = "<b><color=#ff6b6b>{0}</color></b> est le <b>RÉALISATEUR</b> ! Tape <b>/help</b>";
        public const string FirstDirectorPlain = "{0} est le REALISATEUR ! (/help)";
        public const string Discord = "<b><color=#ffd23f>Discord</color></b> : imaginaryconception ou kalinina_sn";
        public const string DiscordPlain = "Discord : imaginaryconception ou kalinina_sn";

        public const string SetImpostorSuccess = "<b><color=#ff6b6b>{0}</color></b> est désormais Imposteur !";
        public const string SetImpostorSuccessPlain = "{0} est desormais Imposteur !";

        public const string UsageSetImpostor = "<b>Usage :</b> /setimpostor ID";
        public const string UsageSetImpostorPlain = "Usage : /setimpostor ID";

        public const string GameStopped = "<b><color=#ff6b6b>STOP</color></b> — Partie arrêtée !";
        public const string GameStoppedPlain = "STOP - Partie arretee !";

        public const string NoGameRunning = "<color=#ffd23f>Aucune partie en cours !</color>";
        public const string NoGameRunningPlain = "Aucune partie en cours !";

        public const string KillSuccess = "<b><color=#ff6b6b>{0}</color></b> a été éliminé !";
        public const string KillSuccessPlain = "{0} a ete elimine !";

        public const string UsageKill = "<b>Usage :</b> /kill ID (ex : /kill A)";
        public const string UsageKillPlain = "Usage : /kill ID (ex : /kill A)";

        public const string UsageRename = "<b>Usage :</b> /rename ID NOUVEAU_NOM (ex : /rename A Bob)";
        public const string UsageRenamePlain = "Usage : /rename ID NOUVEAU_NOM (ex : /rename A Bob)";

        public const string RenameDone = "<b><color=#00e676>Renommé</color></b> : {0} → <b>{1}</b>";
        public const string RenameDonePlain = "Renomme : {0} -> {1}";

        public const string MeetingEnded = "<b><color=#ff4d4d>Réunion</color></b> forcée à se terminer !";
        public const string MeetingEndedPlain = "Reunion forcee a se terminer !";

        // ===== /help : UN SEUL message complet et stylisé =====
        public const string HelpAll =
            "<b><color=#ff6b6b>══ THE DIRECTOR'S CUT ══</color></b>\n" +
            "<b><color=#ffd23f>Général</color></b> : /help · /welcome · /gg · /players · /join · /discord · /cooldowns\n" +
            "<b><color=#ffd23f>Réalisateur · jeu</color></b> : /cut · /darkness · /freeze A · /randomcolors · /colorblinds · /shuffle · /swap A B · /teleportall A · /tp A B\n" +
            "<b><color=#ffd23f>Réalisateur · réunion</color></b> : /action A X · /loc A Z · /vote A B\n" +
            "<b><color=#3B9DFF>Directives · jeu</color></b> : /voiceover txt · /spotlight A · /marathon · /quarantine A · /curse A · /roulette · /bodyswap A B · /cube bonus|malus\n" +
            "<b><color=#3B9DFF>Directives · réunion</color></b> : /stalker A B · /pacifist A · /stockholm C I · /eject first|last\n" +
            "<b><color=#b2bec3>Détails</color></b> : /hcut · /hloc · /haction · /hvote · /hfreeze · /hdarkness";
        public const string HelpAllPlain =
            "== THE DIRECTOR'S CUT ==\n" +
            "General : /help /welcome /gg /players /join /discord /cooldowns\n" +
            "Realisateur jeu : /cut /darkness /freeze A /randomcolors /colorblinds /shuffle /swap A B /teleportall A /tp A B\n" +
            "Realisateur reunion : /action A X /loc A Z /vote A B\n" +
            "Directives jeu : /voiceover txt /spotlight A /marathon /quarantine A /curse A /roulette /bodyswap A B /cube bonus|malus\n" +
            "Directives reunion : /stalker A B /pacifist A /stockholm C I /eject first|last\n" +
            "Details : /hcut /hloc /haction /hvote /hfreeze /hdarkness";

        // Ligne Admin ajoutée au message /help uniquement pour l'hôte
        public const string HelpAdminLine =
            "<b><color=#ff4d4d>Admin (hôte)</color></b> : /start · /stop · /setdirector A · /rename A nom · /kill A · /kick A · /endmeeting · /status · <i>[Suppr] = panneau</i>";
        public const string HelpAdminLinePlain =
            "Admin (hote) : /start /stop /setdirector A /rename A nom /kill A /kick A /endmeeting /status [Suppr] = panneau";

        // Section Directives (Réalisateur) affichée dans /help
        public const string HelpDirectives =
            "<b><color=#ff6b6b>Directives du Réalisateur</color></b>\n" +
            "<b>En jeu :</b> /voiceover &lt;texte&gt;, /spotlight A, /marathon, /quarantine A, /curse A, /roulette, /bodyswap A B, /cube bonus|malus\n" +
            "<b>En réunion :</b> /stalker A B, /pacifist A, /stockholm CREW IMP, /eject first|last";
        public const string HelpDirectivesPlain =
            "Directives du Realisateur\n" +
            "En jeu : /voiceover <texte>, /spotlight A, /marathon, /quarantine A, /curse A, /roulette, /bodyswap A B, /cube bonus|malus\n" +
            "En reunion : /stalker A B, /pacifist A, /stockholm CREW IMP, /eject first|last";

        // Section Admin affichée dans /help (hôte uniquement)
        public const string HelpAdmin =
            "<b><color=#ff4d4d>Admin (hôte)</color></b>\n" +
            "/start — lance la partie  •  /stop — arrête la partie\n" +
            "/setdirector [ID] — désigne le Réalisateur\n" +
            "/rename ID NOM — renomme un joueur\n" +
            "/kill ID — élimine un joueur\n" +
            "/endmeeting — termine la réunion en cours\n" +
            "<i>(touche Suppr / Delete : ouvre le panneau Admin)</i>";
        public const string HelpAdminPlain =
            "Admin (hote)\n" +
            "/start - lance la partie  -  /stop - arrete la partie\n" +
            "/setdirector [ID] - designe le Realisateur\n" +
            "/rename ID NOM - renomme un joueur\n" +
            "/kill ID - elimine un joueur\n" +
            "/endmeeting - termine la reunion en cours\n" +
            "(touche Suppr / Delete : ouvre le panneau Admin)";

        public const string CutStart = "<b><color=#ff6b6b>CUT !</color></b> Sabotage réacteur (2s) → <b>ARRÊT</b> (5s) :\n<b>NE BOUGEZ PLUS</b> — tous les bougeurs sont éliminés !";
        public const string CutStartPlain = "CUT ! Sabotage reacteur (2s) -> ARRET (5s) : NE BOUGEZ PLUS, tous les bougeurs sont elimines !";

        public const string CutEliminated = "<b><color=#ff6b6b>{0}</color></b> a bougé — <b>éliminé !</b>";
        public const string CutEliminatedPlain = "{0} a bouge - elimine !";

        public const string DarknessStart = "<b><color=#2d3436>DARKNESS !</color></b> NOIR TOTAL pendant 10s !";
        public const string DarknessStartPlain = "DARKNESS ! NOIR TOTAL pendant 10s !";

        public const string DarknessEnd = "<b><color=#ffd23f>LUMIÈRE !</color></b> Retour à la normale !";
        public const string DarknessEndPlain = "LUMIERE ! Retour a la normale !";

        public const string FreezeStart = "<b><color=#74b9ff>FREEZE !</color></b> <b>{0}</b> est bloqué 8s !";
        public const string FreezeStartPlain = "FREEZE ! {0} est bloque 8s !";

        public const string FreezeEnd = "<b><color=#00e676>GO !</color></b> <b>{0}</b> peut à nouveau bouger !";
        public const string FreezeEndPlain = "GO ! {0} peut a nouveau bouger !";

        public const string ColorBlindStart = "<b><color=#b2bec3>COLORBLIND !</color></b> Tout le monde en gris, noms masqués (25s) !";
        public const string ColorBlindStartPlain = "COLORBLIND ! Tout le monde en gris, noms masques (25s) !";

        public const string ColorBlindEnd = "<b><color=#ffd23f>RETOUR !</color></b> Couleurs et noms rétablis !";
        public const string ColorBlindEndPlain = "RETOUR ! Couleurs et noms retablis !";

        public const string ShuffleStart = "<b><color=#a29bfe>SHUFFLE !</color></b> Positions mélangées au hasard !";
        public const string ShuffleStartPlain = "SHUFFLE ! Positions melangees au hasard !";

        public const string SwapDone = "<b><color=#a29bfe>SWAP !</color></b> <b>{0}</b> et <b>{1}</b> ont échangé leurs positions !";
        public const string SwapDonePlain = "SWAP ! {0} et {1} ont echange leurs positions !";

        public const string TeleportAllDone = "<b><color=#a29bfe>TÉLÉPORT !</color></b> Tout le monde a été téléporté vers <b>{0}</b> !";
        public const string TeleportAllDonePlain = "TELEPORT ! Tout le monde a ete teleporte vers {0} !";
    }
}
