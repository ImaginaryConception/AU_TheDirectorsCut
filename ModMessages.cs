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

        // ---- /hloc : un seul message ----
        public const string HelpLoc = "<b><color=#ffd23f>/loc LETTRE ZONE</color></b> — Interdit une zone à un joueur. Cooldown 20s";
        public const string HelpLocPlain = "/loc LETTRE ZONE - Interdit une zone a un joueur. Cooldown 20s";
        public const string HelpLocFull =
            "<b><color=#ffd23f>/loc LETTRE ZONE</color></b> — interdit au joueur LETTRE d'entrer dans ZONE pendant la manche (Skeld). <b>S'il y entre, il est éliminé.</b> Le joueur est prévenu en privé. <i>Réunion. Cooldown 20s.</i>\n" +
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

        public const string HelpVote =
            "<b><color=#ffd23f>/vote LETTRE CIBLE</color></b> — force le joueur <b>LETTRE</b> à voter pour <b>CIBLE</b> lors de la réunion en cours.\n" +
            "S'il vote pour quelqu'un d'autre, s'il passe (skip) ou ne vote pas, il est <b>éliminé</b> à la fin de la réunion.\n" +
            "Le joueur reçoit l'ordre en privé. Ex : <b>/vote A B</b> (A doit voter B).\n" +
            "<i>En réunion uniquement. Cooldown 20s.</i>";
        public const string HelpVotePlain = "/vote LETTRE CIBLE - force LETTRE a voter CIBLE ce vote ; sinon elimine. Ex: /vote A B. En reunion. Cooldown 20s.";

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

        public const string HelpRandomColors =
            "<b><color=#ffd23f>/randomcolors</color></b> — donne instantanément à chaque joueur vivant une couleur <b>aléatoire et unique</b> (mélange général).\n" +
            "Le changement est permanent jusqu'au prochain changement de couleur (autre /randomcolors, /colorblind, /bodyswap…).\n" +
            "<i>En jeu uniquement. Cooldown 20s.</i>";
        public const string HelpRandomColorsPlain = "/randomcolors - couleur aleatoire et unique pour chaque joueur vivant (permanent). En jeu. Cooldown 20s.";

        public const string HelpCut =
            "<b><color=#ff6b6b>/cut</color></b> — le jeu du « 1, 2, 3, Soleil ».\n" +
            "1) Un sabotage réacteur de <b>2s</b> sert de signal d'alerte.\n" +
            "2) Phase d'<b>ARRÊT de 5s</b> : pendant ces 5 secondes, <b>tout joueur qui se déplace</b> (plus d'un demi-pas) est <b>éliminé immédiatement</b>.\n" +
            "3) Un dernier sabotage de 2s indique qu'on peut <b>rebouger</b>.\n" +
            "<i>En jeu uniquement. Cooldown 30s.</i>";
        public const string HelpCutPlain = "/cut - 1,2,3 soleil : alerte 2s, puis arret 5s ou tout mouvement = mort, puis 2s avant de rebouger. En jeu. Cooldown 30s.";

        public const string HelpDarkness =
            "<b><color=#2d3436>/darkness</color></b> — coupe la <b>vision de TOUS</b> les joueurs (lumière à zéro, écran quasi noir) pendant <b>10s</b>, puis la vision revient automatiquement.\n" +
            "N'affecte que la visibilité, pas la vitesse de déplacement.\n" +
            "<i>En jeu uniquement. Cooldown 35s.</i>";
        public const string HelpDarknessPlain = "/darkness - coupe la vision de tous (ecran noir) 10s, puis retour auto. En jeu. Cooldown 35s.";

        public const string HelpFreeze =
            "<b><color=#74b9ff>/freeze LETTRE</color></b> — <b>fige sur place</b> le joueur visé pendant <b>8s</b> (vitesse ~0).\n" +
            "Il ne peut plus se déplacer mais reste vivant et visible. Au bout de 8s il rebouge normalement.\n" +
            "Ex : <b>/freeze A</b>. <i>En jeu uniquement. Cooldown 30s.</i>";
        public const string HelpFreezePlain = "/freeze LETTRE - fige le joueur 8s (ne peut plus bouger), reste vivant. Ex: /freeze A. En jeu. Cooldown 30s.";

        public const string HelpAction = "<b><color=#ffd23f>/action LETTRE SCRIPT</color></b> — Donne un script secret à un joueur ! Cooldown 20s";
        public const string HelpActionPlain = "/action LETTRE SCRIPT - Donne un script secret a un joueur ! Cooldown 20s";

        public const string ActionList = "<b><color=#ffd23f>SCRIPTS</color></b> : <color=#3B9DFF>A</color>=NoReport, <color=#3B9DFF>B</color>=SkipVote, <color=#3B9DFF>C</color>=NoVents, <color=#3B9DFF>D</color>=VoteFirst";
        public const string ActionListPlain = "SCRIPTS: A=NoReport, B=SkipVote, C=NoVents, D=VoteFirst";

        // ---- /haction et /helpaction : un seul message ----
        public const string HelpActionFull =
            "<b><color=#ffd23f>/action LETTRE SCRIPT</color></b> — donne un <b>ordre secret</b> au joueur LETTRE pour la manche qui suit. Le joueur est prévenu en privé ; <b>s'il désobéit, il est éliminé</b>.\n" +
            "<b><color=#3B9DFF>A</color> NoReport</b> : ne doit pas signaler de corps ce round.\n" +
            "<b><color=#3B9DFF>B</color> SkipVote</b> : doit passer (skip) son vote à la prochaine réunion.\n" +
            "<b><color=#3B9DFF>C</color> NoVents</b> : ne doit pas utiliser les vents ce round.\n" +
            "<b><color=#3B9DFF>D</color> VoteFirst</b> : doit être le tout premier à voter.\n" +
            "Ex : <b>/action A B</b> (donne SkipVote à A). <i>En réunion uniquement. Cooldown 20s.</i>";
        public const string HelpActionFullPlain =
            "/action LETTRE SCRIPT - ordre secret au joueur ; desobeir = elimine.\n" +
            "A NoReport : ne doit pas signaler de corps.\n" +
            "B SkipVote : doit passer son vote.\n" +
            "C NoVents : ne doit pas utiliser les vents.\n" +
            "D VoteFirst : doit voter en premier.\n" +
            "Ex : /action A B. En reunion. Cooldown 20s.";

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

        // Pseudos à ajouter (pour ceux qui préfèrent ajouter directement plutôt que copier le lien)
        public const string DiscordContacts = "<b>Ajouts directs</b> : imaginaryconception · kalinina_sn";
        public const string DiscordContactsPlain = "Ajouts directs : imaginaryconception · kalinina_sn";

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
            "<b><color=#ffd23f>Général</color></b> : /help · /welcome · /gg · /players · /discord · /cooldowns\n" +
            "<b><color=#ffd23f>Réalisateur</color></b>\n" +
            "/cut · /darkness · /freeze A · /randomcolors · /colorblind\n" +
            "/shuffle · /swap A B · /teleportall A · /tp A B\n" +
            "/voiceover txt · /spotlight A · /marathon · /quarantine A\n" +
            "/roulette · /bodyswap A B\n" +
            "/action A X · /loc A Z · /vote A B <i>(réunion)</i>\n" +
            "/stalker A B · /ultimatum A [s] <i>(réunion)</i>\n" +
            "<b><color=#b2bec3>Détails</color></b> : /h + commande (ex : /hcut, /haction, /hloc)";
        public const string HelpAllPlain =
            "== THE DIRECTOR'S CUT ==\n" +
            "General : /help /welcome /gg /players /discord /cooldowns\n" +
            "Realisateur :\n" +
            "/cut /darkness /freeze A /randomcolors /colorblind\n" +
            "/shuffle /swap A B /teleportall A /tp A B\n" +
            "/voiceover txt /spotlight A /marathon /quarantine A\n" +
            "/roulette /bodyswap A B\n" +
            "/action A X /loc A Z /vote A B (reunion)\n" +
            "/stalker A B /ultimatum A [s] (reunion)\n" +
            "Details : /h + commande (ex : /hcut, /haction, /hloc)";

        // Ligne Admin ajoutée au message /help uniquement pour l'hôte
        public const string HelpAdminLine =
            "<b><color=#ff4d4d>Admin (hôte)</color></b> : /start · /stop · /setdirector A · /rename A nom · /kill A · /kick A · /endmeeting · /status · <i>[Suppr] = panneau</i>";
        public const string HelpAdminLinePlain =
            "Admin (hote) : /start /stop /setdirector A /rename A nom /kill A /kick A /endmeeting /status [Suppr] = panneau";

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
