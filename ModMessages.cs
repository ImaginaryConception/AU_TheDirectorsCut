using System;
using System.Collections.Generic;

namespace AU_TheDirectorsCut
{
    public static class ModMessages
    {
        public static string Welcome => Localization.Pick(
            "<b><color=#ff6b6b>THE DIRECTOR'S CUT</color></b>\nThe first player to die becomes the <b><color=#ffd23f>DIRECTOR</color></b>.\nType <b>/help</b> to see all the commands!",
            "<b><color=#ff6b6b>THE DIRECTOR'S CUT</color></b>\nLe premier mort devient <b><color=#ffd23f>RÉALISATEUR</color></b>.\nTape <b>/help</b> pour voir toutes les commandes !");
        public static string WelcomePlain => Localization.Pick(
            "THE DIRECTOR'S CUT - The first player to die becomes the DIRECTOR. Type /help to see the commands!",
            "THE DIRECTOR'S CUT - Le premier mort devient REALISATEUR. Tape /help pour voir les commandes !");

        public static string HelpLoc => Localization.Pick(
            "<b><color=#ffd23f>/loc LETTER ZONE</color></b> — Forbids a zone for a player. Cooldown 20s",
            "<b><color=#ffd23f>/loc LETTRE ZONE</color></b> — Interdit une zone à un joueur. Cooldown 20s");
        public static string HelpLocPlain => Localization.Pick(
            "/loc LETTER ZONE - Forbids a zone for a player. Cooldown 20s",
            "/loc LETTRE ZONE - Interdit une zone a un joueur. Cooldown 20s");
        public static string HelpLocFull => Localization.Pick(
            "<b><color=#ffd23f>/loc LETTER ZONE</color></b> — forbids player LETTER from entering ZONE during the round (Skeld). <b>If they enter, they are eliminated.</b> The player is warned privately. <i>Meeting. Cooldown 20s.</i>\n" +
            "<b>Zones:</b>\n" +
            "<color=#3B9DFF>B</color>=Admin  <color=#3B9DFF>C</color>=Electrical  <color=#3B9DFF>D</color>=Storage  <color=#3B9DFF>E</color>=Security  <color=#3B9DFF>F</color>=Reactor\n" +
            "<color=#3B9DFF>G</color>=UpperEngine  <color=#3B9DFF>H</color>=LowerEngine  <color=#3B9DFF>I</color>=Medbay  <color=#3B9DFF>J</color>=Communications\n" +
            "<color=#3B9DFF>K</color>=Shields  <color=#3B9DFF>L</color>=O2  <color=#3B9DFF>M</color>=Navigation  <color=#3B9DFF>N</color>=Weapons\n" +
            "Ex: <b>/loc A B</b>",
            "<b><color=#ffd23f>/loc LETTRE ZONE</color></b> — interdit au joueur LETTRE d'entrer dans ZONE pendant la manche (Skeld). <b>S'il y entre, il est éliminé.</b> Le joueur est prévenu en privé. <i>Réunion. Cooldown 20s.</i>\n" +
            "<b>Zones :</b>\n" +
            "<color=#3B9DFF>B</color>=Admin  <color=#3B9DFF>C</color>=Electrical  <color=#3B9DFF>D</color>=Storage  <color=#3B9DFF>E</color>=Security  <color=#3B9DFF>F</color>=Réacteur\n" +
            "<color=#3B9DFF>G</color>=UpperEngine  <color=#3B9DFF>H</color>=LowerEngine  <color=#3B9DFF>I</color>=Medbay  <color=#3B9DFF>J</color>=Communications\n" +
            "<color=#3B9DFF>K</color>=Shields  <color=#3B9DFF>L</color>=O2  <color=#3B9DFF>M</color>=Navigation  <color=#3B9DFF>N</color>=Weapons\n" +
            "Ex : <b>/loc A B</b>");
        public static string HelpLocFullPlain => Localization.Pick(
            "/loc LETTER ZONE - forbids a zone for a player this round (meeting). Cooldown 20s.\n" +
            "Zones: B=Admin C=Electrical D=Storage E=Security F=Reactor G=UpperEngine H=LowerEngine I=Medbay J=Communications K=Shields L=O2 M=Navigation N=Weapons\n" +
            "Ex: /loc A B",
            "/loc LETTRE ZONE - interdit une zone a un joueur ce round (reunion). Cooldown 20s.\n" +
            "Zones : B=Admin C=Electrical D=Storage E=Security F=Reacteur G=UpperEngine H=LowerEngine I=Medbay J=Communications K=Shields L=O2 M=Navigation N=Weapons\n" +
            "Ex : /loc A B");

        public static string LocList1 => Localization.Pick(
            "<b><color=#ffd23f>Zones (B-G)</color></b>: B=Admin, C=Electrical, D=Storage, E=Security, F=Reactor, G=UpperEngine",
            "<b><color=#ffd23f>Zones (B-G)</color></b> : B=Admin, C=Electrical, D=Storage, E=Security, F=Réacteur, G=UpperEngine");
        public static string LocList1Plain => Localization.Pick(
            "Zones (B-G): B=Admin, C=Electrical, D=Storage, E=Security, F=Reactor, G=UpperEngine",
            "Zones (B-G): B=Admin, C=Electrical, D=Storage, E=Security, F=Reacteur, G=UpperEngine");
        public static string LocList2 => Localization.Pick(
            "<b><color=#ffd23f>Zones (H-N)</color></b>: H=LowerEngine, I=Medbay, J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons",
            "<b><color=#ffd23f>Zones (H-N)</color></b> : H=LowerEngine, I=Medbay, J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons");
        public static string LocList2Plain => Localization.Pick(
            "Zones (H-N): H=LowerEngine, I=Medbay, J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons",
            "Zones (H-N): H=LowerEngine, I=Medbay, J=Communications, K=Shields, L=O2, M=Navigation, N=Weapons");
        public static string LocList => LocList1;
        public static string LocListPlain => LocList1Plain;

        public static string HelpVote => Localization.Pick(
            "<b><color=#ffd23f>/vote LETTER TARGET</color></b> — forces player <b>LETTER</b> to vote for <b>TARGET</b> during the current meeting.\n" +
            "If they vote for someone else, skip, or don't vote, they are <b>eliminated</b> at the end of the meeting.\n" +
            "The player receives the order privately. Ex: <b>/vote A B</b> (A must vote B).\n" +
            "<i>During meetings only. Cooldown 20s.</i>",
            "<b><color=#ffd23f>/vote LETTRE CIBLE</color></b> — force le joueur <b>LETTRE</b> à voter pour <b>CIBLE</b> lors de la réunion en cours.\n" +
            "S'il vote pour quelqu'un d'autre, s'il passe (skip) ou ne vote pas, il est <b>éliminé</b> à la fin de la réunion.\n" +
            "Le joueur reçoit l'ordre en privé. Ex : <b>/vote A B</b> (A doit voter B).\n" +
            "<i>En réunion uniquement. Cooldown 20s.</i>");
        public static string HelpVotePlain => Localization.Pick(
            "/vote LETTER TARGET - forces LETTER to vote TARGET this vote; otherwise eliminated. Ex: /vote A B. During meeting. Cooldown 20s.",
            "/vote LETTRE CIBLE - force LETTRE a voter CIBLE ce vote ; sinon elimine. Ex: /vote A B. En reunion. Cooldown 20s.");

        public static string UsageLoc => Localization.Pick(
            "<b>Usage:</b> /loc LETTER ZONE (ex: /loc A B = forbids zone B for player A)",
            "<b>Usage :</b> /loc LETTRE ZONE (ex : /loc A B = interdit la zone B au joueur A)");
        public static string UsageLocPlain => Localization.Pick(
            "Usage: /loc LETTER ZONE (ex: /loc A B)",
            "Usage: /loc LETTRE ZONE (ex: /loc A B)");

        public static string UsageVote => Localization.Pick(
            "<b>Usage:</b> /vote LETTER TARGET (ex: /vote A B = forces A to vote B)",
            "<b>Usage :</b> /vote LETTRE CIBLE (ex : /vote A B = force A à voter B)");
        public static string UsageVotePlain => Localization.Pick(
            "Usage: /vote LETTER TARGET (ex: /vote A B)",
            "Usage: /vote LETTRE CIBLE (ex: /vote A B)");

        public static string LocAssigned => Localization.Pick(
            "<b><color=#00e676>/loc</color></b> — Order sent to <b>{0}</b>!",
            "<b><color=#00e676>/loc</color></b> — Ordre envoyé à <b>{0}</b> !");
        public static string LocAssignedPlain => Localization.Pick(
            "/loc: Order sent to {0}!",
            "/loc: Ordre envoye a {0} !");

        public static string VoteAssigned => Localization.Pick(
            "<b><color=#00e676>/vote</color></b> — Order sent to <b>{0}</b>!",
            "<b><color=#00e676>/vote</color></b> — Ordre envoyé à <b>{0}</b> !");
        public static string VoteAssignedPlain => Localization.Pick(
            "/vote: Order sent to {0}!",
            "/vote: Ordre envoye a {0} !");

        public static string OnlyInMeeting => Localization.Pick(
            "<color=#ff6b6b>This command can only be used in a <b>meeting</b>!</color>",
            "<color=#ff6b6b>Cette commande ne s'utilise qu'en <b>réunion</b> !</color>");
        public static string OnlyInMeetingPlain => Localization.Pick(
            "This command can only be used in a meeting!",
            "Cette commande ne s'utilise qu'en reunion !");

        public static string HelpRandomColors => Localization.Pick(
            "<b><color=#ffd23f>/randomcolors</color></b> — instantly gives each living player a <b>random and unique</b> color (general shuffle).\n" +
            "The change is permanent until the next color change (another /randomcolors, /colorblind, /bodyswap…).\n" +
            "<i>In game only. Cooldown 20s.</i>",
            "<b><color=#ffd23f>/randomcolors</color></b> — donne instantanément à chaque joueur vivant une couleur <b>aléatoire et unique</b> (mélange général).\n" +
            "Le changement est permanent jusqu'au prochain changement de couleur (autre /randomcolors, /colorblind, /bodyswap…).\n" +
            "<i>En jeu uniquement. Cooldown 20s.</i>");
        public static string HelpRandomColorsPlain => Localization.Pick(
            "/randomcolors - random and unique color for each living player (permanent). In game. Cooldown 20s.",
            "/randomcolors - couleur aleatoire et unique pour chaque joueur vivant (permanent). En jeu. Cooldown 20s.");

        public static string HelpCut => Localization.Pick(
            "<b><color=#ff6b6b>/cut</color></b> — the game of \"Red Light, Green Light\".\n" +
            "1) A <b>2s</b> reactor sabotage acts as the alert signal.\n" +
            "2) <b>STOP phase of 5s</b>: during these 5 seconds, <b>any player who moves</b> (more than half a step) is <b>eliminated immediately</b>.\n" +
            "3) A final 2s sabotage means you can <b>move again</b>.\n" +
            "<i>In game only. Cooldown 30s.</i>",
            "<b><color=#ff6b6b>/cut</color></b> — le jeu du « 1, 2, 3, Soleil ».\n" +
            "1) Un sabotage réacteur de <b>2s</b> sert de signal d'alerte.\n" +
            "2) Phase d'<b>ARRÊT de 5s</b> : pendant ces 5 secondes, <b>tout joueur qui se déplace</b> (plus d'un demi-pas) est <b>éliminé immédiatement</b>.\n" +
            "3) Un dernier sabotage de 2s indique qu'on peut <b>rebouger</b>.\n" +
            "<i>En jeu uniquement. Cooldown 30s.</i>");
        public static string HelpCutPlain => Localization.Pick(
            "/cut - red light green light: 2s alert, then 5s stop where any movement = death, then 2s before moving again. In game. Cooldown 30s.",
            "/cut - 1,2,3 soleil : alerte 2s, puis arret 5s ou tout mouvement = mort, puis 2s avant de rebouger. En jeu. Cooldown 30s.");

        public static string HelpDarkness => Localization.Pick(
            "<b><color=#2d3436>/darkness</color></b> — cuts the <b>vision of ALL</b> players (light to zero, near-black screen) for <b>10s</b>, then vision returns automatically.\n" +
            "Only affects visibility, not movement speed.\n" +
            "<i>In game only. Cooldown 35s.</i>",
            "<b><color=#2d3436>/darkness</color></b> — coupe la <b>vision de TOUS</b> les joueurs (lumière à zéro, écran quasi noir) pendant <b>10s</b>, puis la vision revient automatiquement.\n" +
            "N'affecte que la visibilité, pas la vitesse de déplacement.\n" +
            "<i>En jeu uniquement. Cooldown 35s.</i>");
        public static string HelpDarknessPlain => Localization.Pick(
            "/darkness - cuts everyone's vision (black screen) 10s, then auto return. In game. Cooldown 35s.",
            "/darkness - coupe la vision de tous (ecran noir) 10s, puis retour auto. En jeu. Cooldown 35s.");

        public static string HelpFreeze => Localization.Pick(
            "<b><color=#74b9ff>/freeze LETTER</color></b> — <b>freezes in place</b> the targeted player for <b>8s</b> (speed ~0).\n" +
            "They can no longer move but stay alive and visible. After 8s they move normally again.\n" +
            "Ex: <b>/freeze A</b>. <i>In game only. Cooldown 30s.</i>",
            "<b><color=#74b9ff>/freeze LETTRE</color></b> — <b>fige sur place</b> le joueur visé pendant <b>8s</b> (vitesse ~0).\n" +
            "Il ne peut plus se déplacer mais reste vivant et visible. Au bout de 8s il rebouge normalement.\n" +
            "Ex : <b>/freeze A</b>. <i>En jeu uniquement. Cooldown 30s.</i>");
        public static string HelpFreezePlain => Localization.Pick(
            "/freeze LETTER - freezes the player 8s (cannot move), stays alive. Ex: /freeze A. In game. Cooldown 30s.",
            "/freeze LETTRE - fige le joueur 8s (ne peut plus bouger), reste vivant. Ex: /freeze A. En jeu. Cooldown 30s.");

        public static string HelpAction => Localization.Pick(
            "<b><color=#ffd23f>/action LETTER SCRIPT</color></b> — Gives a secret script to a player! Cooldown 20s",
            "<b><color=#ffd23f>/action LETTRE SCRIPT</color></b> — Donne un script secret à un joueur ! Cooldown 20s");
        public static string HelpActionPlain => Localization.Pick(
            "/action LETTER SCRIPT - Gives a secret script to a player! Cooldown 20s",
            "/action LETTRE SCRIPT - Donne un script secret a un joueur ! Cooldown 20s");

        public static string ActionList => Localization.Pick(
            "<b><color=#ffd23f>SCRIPTS</color></b>: <color=#3B9DFF>A</color>=NoReport, <color=#3B9DFF>B</color>=SkipVote, <color=#3B9DFF>C</color>=NoVents, <color=#3B9DFF>D</color>=VoteFirst",
            "<b><color=#ffd23f>SCRIPTS</color></b> : <color=#3B9DFF>A</color>=NoReport, <color=#3B9DFF>B</color>=SkipVote, <color=#3B9DFF>C</color>=NoVents, <color=#3B9DFF>D</color>=VoteFirst");
        public static string ActionListPlain => Localization.Pick(
            "SCRIPTS: A=NoReport, B=SkipVote, C=NoVents, D=VoteFirst",
            "SCRIPTS: A=NoReport, B=SkipVote, C=NoVents, D=VoteFirst");

        public static string HelpActionFull => Localization.Pick(
            "<b><color=#ffd23f>/action LETTER SCRIPT</color></b> — gives a <b>secret order</b> to player LETTER for the upcoming round. The player is warned privately; <b>if they disobey, they are eliminated</b>.\n" +
            "<b><color=#3B9DFF>A</color> NoReport</b>: must not report a body this round.\n" +
            "<b><color=#3B9DFF>B</color> SkipVote</b>: must skip their vote at the next meeting.\n" +
            "<b><color=#3B9DFF>C</color> NoVents</b>: must not use vents this round.\n" +
            "<b><color=#3B9DFF>D</color> VoteFirst</b>: must be the very first to vote.\n" +
            "Ex: <b>/action A B</b> (gives SkipVote to A). <i>During meetings only. Cooldown 20s.</i>",
            "<b><color=#ffd23f>/action LETTRE SCRIPT</color></b> — donne un <b>ordre secret</b> au joueur LETTRE pour la manche qui suit. Le joueur est prévenu en privé ; <b>s'il désobéit, il est éliminé</b>.\n" +
            "<b><color=#3B9DFF>A</color> NoReport</b> : ne doit pas signaler de corps ce round.\n" +
            "<b><color=#3B9DFF>B</color> SkipVote</b> : doit passer (skip) son vote à la prochaine réunion.\n" +
            "<b><color=#3B9DFF>C</color> NoVents</b> : ne doit pas utiliser les vents ce round.\n" +
            "<b><color=#3B9DFF>D</color> VoteFirst</b> : doit être le tout premier à voter.\n" +
            "Ex : <b>/action A B</b> (donne SkipVote à A). <i>En réunion uniquement. Cooldown 20s.</i>");
        public static string HelpActionFullPlain => Localization.Pick(
            "/action LETTER SCRIPT - secret order to the player; disobey = eliminated.\n" +
            "A NoReport: must not report a body.\n" +
            "B SkipVote: must skip their vote.\n" +
            "C NoVents: must not use vents.\n" +
            "D VoteFirst: must vote first.\n" +
            "Ex: /action A B. During meeting. Cooldown 20s.",
            "/action LETTRE SCRIPT - ordre secret au joueur ; desobeir = elimine.\n" +
            "A NoReport : ne doit pas signaler de corps.\n" +
            "B SkipVote : doit passer son vote.\n" +
            "C NoVents : ne doit pas utiliser les vents.\n" +
            "D VoteFirst : doit voter en premier.\n" +
            "Ex : /action A B. En reunion. Cooldown 20s.");

        public static string HelpActionTitle => Localization.Pick(
            "<b><color=#ffd23f>/helpaction — Detailed list of scripts</color></b>",
            "<b><color=#ffd23f>/helpaction — Liste détaillée des scripts</color></b>");
        public static string HelpActionTitlePlain => Localization.Pick(
            "/helpaction - Detailed list of scripts",
            "/helpaction - Liste detaillee des scripts");
        public static string HelpActionA => Localization.Pick(
            "<b><color=#3B9DFF>A / NoReport</color></b>: You must not report a body this round!",
            "<b><color=#3B9DFF>A / NoReport</color></b> : Tu ne dois pas signaler de corps ce round !");
        public static string HelpActionAPlain => Localization.Pick(
            "A / NoReport: You must not report a body this round!",
            "A / NoReport: Tu ne dois pas signaler de corps ce round !");
        public static string HelpActionB => Localization.Pick(
            "<b><color=#3B9DFF>B / SkipVote</color></b>: You must skip your vote this round!",
            "<b><color=#3B9DFF>B / SkipVote</color></b> : Tu dois passer ton vote ce round !");
        public static string HelpActionBPlain => Localization.Pick(
            "B / SkipVote: You must skip your vote this round!",
            "B / SkipVote: Tu dois passer ton vote ce round !");
        public static string HelpActionC => Localization.Pick(
            "<b><color=#3B9DFF>C / NoVents</color></b>: You must not use vents this round!",
            "<b><color=#3B9DFF>C / NoVents</color></b> : Tu ne dois pas utiliser les vents ce round !");
        public static string HelpActionCPlain => Localization.Pick(
            "C / NoVents: You must not use vents this round!",
            "C / NoVents: Tu ne dois pas utiliser les vents ce round !");
        public static string HelpActionD => Localization.Pick(
            "<b><color=#3B9DFF>D / VoteFirst</color></b>: You must vote FIRST this round!",
            "<b><color=#3B9DFF>D / VoteFirst</color></b> : Tu dois voter en PREMIER ce round !");
        public static string HelpActionDPlain => Localization.Pick(
            "D / VoteFirst: You must vote FIRST this round!",
            "D / VoteFirst: Tu dois voter en PREMIER ce round !");

        public static string ActionAssigned => Localization.Pick(
            "<b><color=#00e676>SCRIPT</color></b> — Order sent to <b>{0}</b>!",
            "<b><color=#00e676>SCRIPT</color></b> — Ordre envoyé à <b>{0}</b> !");
        public static string ActionAssignedPlain => Localization.Pick(
            "SCRIPT: Order sent to {0}!",
            "SCRIPT: Ordre envoye a {0} !");

        public static string ActionAlreadyActive => Localization.Pick(
            "<color=#ff6b6b><b>{0}</b> already has an active script!</color>",
            "<color=#ff6b6b><b>{0}</b> a déjà un script actif !</color>");
        public static string ActionAlreadyActivePlain => Localization.Pick(
            "{0} already has an active script!",
            "{0} a deja un script actif !");

        public static string UsageAction => Localization.Pick(
            "<b>Usage:</b> /action LETTER SCRIPT (ex: /action A B = SkipVote to player A)",
            "<b>Usage :</b> /action LETTRE SCRIPT (ex : /action A B = SkipVote au joueur A)");
        public static string UsageActionPlain => Localization.Pick(
            "Usage: /action LETTER SCRIPT (ex: /action A B)",
            "Usage: /action LETTRE SCRIPT (ex: /action A B)");

        public static string GgNoGame => Localization.Pick(
            "<b><color=#ffd23f>END</color></b> — No previous game",
            "<b><color=#ffd23f>FIN</color></b> — Aucune partie précédente");
        public static string GgNoGamePlain => Localization.Pick(
            "END - No previous game",
            "FIN - Aucune partie precedente");

        public static string GgSimple => Localization.Pick(
            "<b><color=#ffd23f>END</color></b> — Game over. <b>GG!</b>",
            "<b><color=#ffd23f>FIN</color></b> — Partie terminée. <b>GG !</b>");
        public static string GgSimplePlain => Localization.Pick(
            "END - Game over. GG!",
            "FIN - Partie terminee. GG !");

        public static string GgFormat => Localization.Pick(
            "<b><color=#ffd23f>END OF GAME</color></b>\n<b>Director:</b> {2}\n<b><color=#00e676>Alive:</color></b> {0}\n<b><color=#ff6b6b>Eliminated:</color></b> {1}\n<b>GG!</b>",
            "<b><color=#ffd23f>FIN DE PARTIE</color></b>\n<b>Réalisateur :</b> {2}\n<b><color=#00e676>Vivants :</color></b> {0}\n<b><color=#ff6b6b>Éliminés :</color></b> {1}\n<b>GG !</b>");
        public static string GgFormatPlain => Localization.Pick(
            "END - Director: {2} - Alive: {0} - Eliminated: {1} - GG!",
            "FIN - Realisateur : {2} - Vivants : {0} - Elimines : {1} - GG !");

        public static string DirectorSet => Localization.Pick(
            "<b><color=#ffd23f>{0}</color></b> is the Director!",
            "<b><color=#ffd23f>{0}</color></b> est le Réalisateur !");
        public static string DirectorSetPlain => Localization.Pick(
            "{0} is the Director!",
            "{0} est le Realisateur !");

        public static string RandomColorsStart => Localization.Pick(
            "<b><color=#ffd23f>COLORS!</color></b> Random colors for EVERYONE!",
            "<b><color=#ffd23f>COULEURS !</color></b> Couleurs aléatoires pour TOUS !");
        public static string RandomColorsStartPlain => Localization.Pick(
            "Random colors for EVERYONE!",
            "Couleurs aleatoires pour TOUS !");

        public static string CooldownMsg => Localization.Pick(
            "<color=#ffd23f><b>{0}</b> on cooldown — {1}s remaining</color>",
            "<color=#ffd23f><b>{0}</b> en recharge — {1}s restantes</color>");
        public static string CooldownMsgPlain => Localization.Pick(
            "{0} on cooldown - {1}s remaining",
            "{0} en recharge - {1}s restantes");

        public static string HostOnly => Localization.Pick(
            "<color=#ff6b6b><b>Host only!</b></color>",
            "<color=#ff6b6b><b>Hôte seulement !</b></color>");
        public static string HostOnlyPlain => Localization.Pick(
            "Host only!",
            "Hote seulement !");

        public static string PlayerNotFound => Localization.Pick(
            "<color=#ff6b6b>Player not found!</color>",
            "<color=#ff6b6b>Joueur introuvable !</color>");
        public static string PlayerNotFoundPlain => Localization.Pick(
            "Player not found!",
            "Joueur introuvable !");

        public static string NotDirector => Localization.Pick(
            "<color=#ff6b6b><b>{0}</b>: you are not the Director!</color>",
            "<color=#ff6b6b><b>{0}</b> : tu n'es pas le Réalisateur !</color>");
        public static string NotDirectorPlain => Localization.Pick(
            "{0}: you are not the Director!",
            "{0} : tu n'es pas le Realisateur !");

        public static string FirstDirector => Localization.Pick(
            "<b><color=#ff6b6b>{0}</color></b> is the <b>DIRECTOR</b>! Type <b>/help</b>",
            "<b><color=#ff6b6b>{0}</color></b> est le <b>RÉALISATEUR</b> ! Tape <b>/help</b>");
        public static string FirstDirectorPlain => Localization.Pick(
            "{0} is the DIRECTOR! (/help)",
            "{0} est le REALISATEUR ! (/help)");
        public static string Discord => Localization.Pick(
            "<b><color=#ffd23f>Discord</color></b>: imaginaryconception or kalinina_sn",
            "<b><color=#ffd23f>Discord</color></b> : imaginaryconception ou kalinina_sn");
        public static string DiscordPlain => Localization.Pick(
            "Discord: imaginaryconception or kalinina_sn",
            "Discord : imaginaryconception ou kalinina_sn");

        public static string DiscordContacts => Localization.Pick(
            "<b>Direct adds</b>: imaginaryconception · kalinina_sn",
            "<b>Ajouts directs</b> : imaginaryconception · kalinina_sn");
        public static string DiscordContactsPlain => Localization.Pick(
            "Direct adds: imaginaryconception · kalinina_sn",
            "Ajouts directs : imaginaryconception · kalinina_sn");

        public static string SetImpostorSuccess => Localization.Pick(
            "<b><color=#ff6b6b>{0}</color></b> is now an Impostor!",
            "<b><color=#ff6b6b>{0}</color></b> est désormais Imposteur !");
        public static string SetImpostorSuccessPlain => Localization.Pick(
            "{0} is now an Impostor!",
            "{0} est desormais Imposteur !");

        public static string UsageSetImpostor => Localization.Pick(
            "<b>Usage:</b> /setimpostor ID",
            "<b>Usage :</b> /setimpostor ID");
        public static string UsageSetImpostorPlain => Localization.Pick(
            "Usage: /setimpostor ID",
            "Usage : /setimpostor ID");

        public static string GameStopped => Localization.Pick(
            "<b><color=#ff6b6b>STOP</color></b> — Game stopped!",
            "<b><color=#ff6b6b>STOP</color></b> — Partie arrêtée !");
        public static string GameStoppedPlain => Localization.Pick(
            "STOP - Game stopped!",
            "STOP - Partie arretee !");

        public static string NoGameRunning => Localization.Pick(
            "<color=#ffd23f>No game running!</color>",
            "<color=#ffd23f>Aucune partie en cours !</color>");
        public static string NoGameRunningPlain => Localization.Pick(
            "No game running!",
            "Aucune partie en cours !");

        public static string KillSuccess => Localization.Pick(
            "<b><color=#ff6b6b>{0}</color></b> has been eliminated!",
            "<b><color=#ff6b6b>{0}</color></b> a été éliminé !");
        public static string KillSuccessPlain => Localization.Pick(
            "{0} has been eliminated!",
            "{0} a ete elimine !");

        public static string UsageKill => Localization.Pick(
            "<b>Usage:</b> /kill ID (ex: /kill A)",
            "<b>Usage :</b> /kill ID (ex : /kill A)");
        public static string UsageKillPlain => Localization.Pick(
            "Usage: /kill ID (ex: /kill A)",
            "Usage : /kill ID (ex : /kill A)");

        public static string UsageRename => Localization.Pick(
            "<b>Usage:</b> /rename ID NEW_NAME (ex: /rename A Bob)",
            "<b>Usage :</b> /rename ID NOUVEAU_NOM (ex : /rename A Bob)");
        public static string UsageRenamePlain => Localization.Pick(
            "Usage: /rename ID NEW_NAME (ex: /rename A Bob)",
            "Usage : /rename ID NOUVEAU_NOM (ex : /rename A Bob)");

        public static string RenameDone => Localization.Pick(
            "<b><color=#00e676>Renamed</color></b>: {0} → <b>{1}</b>",
            "<b><color=#00e676>Renommé</color></b> : {0} → <b>{1}</b>");
        public static string RenameDonePlain => Localization.Pick(
            "Renamed: {0} -> {1}",
            "Renomme : {0} -> {1}");

        public static string MeetingEnded => Localization.Pick(
            "<b><color=#ff4d4d>Meeting</color></b> forced to end!",
            "<b><color=#ff4d4d>Réunion</color></b> forcée à se terminer !");
        public static string MeetingEndedPlain => Localization.Pick(
            "Meeting forced to end!",
            "Reunion forcee a se terminer !");

        public static string HHelp => Localization.Pick(
            "<b><color=#ffd23f>/help</color></b> — shows the list of all commands in one styled message. The <b>Admin</b> section only appears for the host.\n<b>Usage:</b> /help (no argument). Everywhere, for everyone.",
            "<b><color=#ffd23f>/help</color></b> — affiche la liste de toutes les commandes en un seul message stylé. La section <b>Admin</b> n'apparaît que pour l'hôte.\n<b>Écriture :</b> /help (aucun argument). Partout, pour tout le monde.");
        public static string HHelpPlain => Localization.Pick(
            "/help - lists all commands. Admin section visible only to the host. Usage: /help.",
            "/help - liste toutes les commandes. Section Admin visible seulement par l'hote. Ecriture : /help.");
        public static string HWelcome => Localization.Pick(
            "<b><color=#ffd23f>/welcome</color></b> — shows the mod's welcome message again (the Director concept).\n<b>Usage:</b> /welcome. Everywhere.",
            "<b><color=#ffd23f>/welcome</color></b> — réaffiche le message de bienvenue du mod (le concept du Réalisateur).\n<b>Écriture :</b> /welcome. Partout.");
        public static string HWelcomePlain => Localization.Pick(
            "/welcome - shows the welcome message again. Usage: /welcome.",
            "/welcome - reaffiche le message de bienvenue. Ecriture : /welcome.");
        public static string HGg => Localization.Pick(
            "<b><color=#ffd23f>/gg</color></b> — sends each player the recap of the PREVIOUS GAME: list of survivors, eliminated players and the Director.\n<b>Usage:</b> /gg. Use in the lobby.",
            "<b><color=#ffd23f>/gg</color></b> — envoie à chaque joueur le récap de la PARTIE PRÉCÉDENTE : liste des survivants, des éliminés et le Réalisateur.\n<b>Écriture :</b> /gg. À utiliser au lobby.");
        public static string HGgPlain => Localization.Pick(
            "/gg - sends the recap of the previous game (alive/eliminated/Director). Usage: /gg.",
            "/gg - envoie le recap de la partie precedente (vivants/elimines/Realisateur). Ecriture : /gg.");
        public static string HPlayers => Localization.Pick(
            "<b><color=#ffd23f>/players</color></b> — lists ALL players with their <b>letter-ID</b> (A, B, C…). These letters are used to target commands.\n<b>Usage:</b> /players. Everywhere.",
            "<b><color=#ffd23f>/players</color></b> — liste TOUS les joueurs avec leur <b>lettre-ID</b> (A, B, C…). Ces lettres servent à cibler les commandes.\n<b>Écriture :</b> /players. Partout.");
        public static string HPlayersPlain => Localization.Pick(
            "/players - lists players and their letter-ID (needed to target). Usage: /players.",
            "/players - liste les joueurs et leur lettre-ID (necessaire pour cibler). Ecriture : /players.");
        public static string HDiscord => Localization.Pick(
            "<b><color=#ffd23f>/discord</color></b> (or <b>/join</b>) — shows the Discord server link and the usernames to add.\n<b>Usage:</b> /discord. Everywhere.",
            "<b><color=#ffd23f>/discord</color></b> (ou <b>/join</b>) — affiche le lien du serveur Discord et les pseudos à ajouter.\n<b>Écriture :</b> /discord. Partout.");
        public static string HDiscordPlain => Localization.Pick(
            "/discord (or /join) - shows the Discord link and usernames. Usage: /discord.",
            "/discord (ou /join) - affiche le lien Discord et les pseudos. Ecriture : /discord.");
        public static string HCooldowns => Localization.Pick(
            "<b><color=#ffd23f>/cooldowns</color></b> (or <b>/cd</b>) — shows the state of ALL cooldown commands: <color=#00e676>ready</color> or time remaining.\n<b>Usage:</b> /cooldowns. Everywhere.",
            "<b><color=#ffd23f>/cooldowns</color></b> (ou <b>/cd</b>) — affiche l'état de TOUTES les commandes à recharge : <color=#00e676>prêt</color> ou temps restant.\n<b>Écriture :</b> /cooldowns. Partout.");
        public static string HCooldownsPlain => Localization.Pick(
            "/cooldowns (or /cd) - state of all commands (ready / time remaining). Usage: /cooldowns.",
            "/cooldowns (ou /cd) - etat de toutes les commandes (pret / temps restant). Ecriture : /cooldowns.");

        public static string HColorblind => Localization.Pick(
            "<b><color=#b2bec3>/colorblind</color></b> — turns ALL players gray and replaces their name with \"<b>Anonymous</b>\" for <b>25s</b>; the original colors and names are saved then restored automatically. No one recognizes anyone.\n<b>Usage:</b> /colorblind (no argument). <i>In game only. Cooldown 40s.</i>",
            "<b><color=#b2bec3>/colorblind</color></b> — rend TOUS les joueurs gris et remplace leur pseudo par « <b>Anonyme</b> » pendant <b>25s</b> ; les couleurs et pseudos d'origine sont sauvegardés puis restaurés automatiquement. Plus personne ne se reconnaît.\n<b>Écriture :</b> /colorblind (aucun argument). <i>En jeu uniquement. Cooldown 40s.</i>");
        public static string HColorblindPlain => Localization.Pick(
            "/colorblind - everyone gray + name 'Anonymous' 25s, then restore. Usage: /colorblind. In game. Cooldown 40s.",
            "/colorblind - tout le monde gris + pseudo 'Anonyme' 25s, puis restaure. Ecriture : /colorblind. En jeu. Cooldown 40s.");
        public static string HShuffle => Localization.Pick(
            "<b><color=#a29bfe>/shuffle</color></b> — teleports all living players to <b>randomly shuffled</b> positions (each lands where another was).\n<b>Usage:</b> /shuffle (no argument). <i>In game only. Cooldown 20s.</i>",
            "<b><color=#a29bfe>/shuffle</color></b> — téléporte tous les joueurs vivants à des positions <b>mélangées au hasard</b> (chacun atterrit là où se trouvait un autre).\n<b>Écriture :</b> /shuffle (aucun argument). <i>En jeu uniquement. Cooldown 20s.</i>");
        public static string HShufflePlain => Localization.Pick(
            "/shuffle - randomly shuffles everyone's positions. Usage: /shuffle. In game. Cooldown 20s.",
            "/shuffle - melange aleatoirement les positions de tous. Ecriture : /shuffle. En jeu. Cooldown 20s.");
        public static string HSwap => Localization.Pick(
            "<b><color=#a29bfe>/swap IDA IDB</color></b> — swaps the <b>positions</b> of two players (A goes where B was and vice versa). If they're in a vent, they exit first.\n<b>Usage:</b> /swap A B (the letter-IDs, see /players). <i>In game only. Cooldown 15s.</i>",
            "<b><color=#a29bfe>/swap IDA IDB</color></b> — échange les <b>positions</b> de deux joueurs (A va où était B et inversement). S'ils sont dans un vent, ils en sortent d'abord.\n<b>Écriture :</b> /swap A B (les lettres-ID, voir /players). <i>En jeu uniquement. Cooldown 15s.</i>");
        public static string HSwapPlain => Localization.Pick(
            "/swap IDA IDB - swaps the positions of two players. Usage: /swap A B. In game. Cooldown 15s.",
            "/swap IDA IDB - echange les positions de deux joueurs. Ecriture : /swap A B. En jeu. Cooldown 15s.");
        public static string HTeleportall => Localization.Pick(
            "<b><color=#a29bfe>/teleportall ID</color></b> — teleports <b>EVERYONE</b> around player ID.\n<b>Usage:</b> /teleportall A (the target's letter-ID). <i>In game only. Cooldown 20s.</i>",
            "<b><color=#a29bfe>/teleportall ID</color></b> — téléporte <b>TOUT le monde</b> autour du joueur ID.\n<b>Écriture :</b> /teleportall A (la lettre-ID de la cible). <i>En jeu uniquement. Cooldown 20s.</i>");
        public static string HTeleportallPlain => Localization.Pick(
            "/teleportall ID - teleports everyone to player ID. Usage: /teleportall A. In game. Cooldown 20s.",
            "/teleportall ID - teleporte tout le monde vers le joueur ID. Ecriture : /teleportall A. En jeu. Cooldown 20s.");
        public static string HTp => Localization.Pick(
            "<b><color=#a29bfe>/tp IDA IDB</color></b> — teleports player <b>A to B</b> (only A moves).\n<b>Usage:</b> /tp A B. <i>In game only. Cooldown 10s.</i>",
            "<b><color=#a29bfe>/tp IDA IDB</color></b> — téléporte le joueur <b>A vers B</b> (A seulement se déplace).\n<b>Écriture :</b> /tp A B. <i>En jeu uniquement. Cooldown 10s.</i>");
        public static string HTpPlain => Localization.Pick(
            "/tp IDA IDB - teleports A to B. Usage: /tp A B. In game. Cooldown 10s.",
            "/tp IDA IDB - teleporte A vers B. Ecriture : /tp A B. En jeu. Cooldown 10s.");
        public static string HVoiceover => Localization.Pick(
            "<b><color=#000000>/voiceover &lt;text&gt;</color></b> — shows an <b>anonymous, theatrical message</b> in large text to EVERYONE in the chat (the Voiceover, in black).\n<b>Usage:</b> /voiceover followed by your text. Ex: <b>/voiceover One of you lied</b>. <i>Everywhere. Cooldown 8s.</i>",
            "<b><color=#000000>/voiceover &lt;texte&gt;</color></b> — affiche un <b>message anonyme et théâtral</b> en grand à TOUS dans le chat (la Voix Off, en noir).\n<b>Écriture :</b> /voiceover suivi de ton texte. Ex : <b>/voiceover L'un d'entre vous a menti</b>. <i>Partout. Cooldown 8s.</i>");
        public static string HVoiceoverPlain => Localization.Pick(
            "/voiceover <text> - anonymous message in large text to all. Usage: /voiceover your text. Cooldown 8s.",
            "/voiceover <texte> - message anonyme en grand a tous. Ecriture : /voiceover ton texte. Cooldown 8s.");
        public static string HSpotlight => Localization.Pick(
            "<b><color=#ffd23f>/spotlight ID</color></b> — plunges everyone into <b>darkness EXCEPT</b> player ID (a spotlight on them) for <b>20s</b>.\n<b>Usage:</b> /spotlight A. <i>In game only. Cooldown 30s.</i>",
            "<b><color=#ffd23f>/spotlight ID</color></b> — plonge tout le monde dans le <b>noir SAUF</b> le joueur ID (un projecteur sur lui) pendant <b>20s</b>.\n<b>Écriture :</b> /spotlight A. <i>En jeu uniquement. Cooldown 30s.</i>");
        public static string HSpotlightPlain => Localization.Pick(
            "/spotlight ID - everyone in darkness except ID, 20s. Usage: /spotlight A. In game. Cooldown 30s.",
            "/spotlight ID - tout le monde dans le noir sauf ID, 20s. Ecriture : /spotlight A. En jeu. Cooldown 30s.");
        public static string HMarathon => Localization.Pick(
            "<b><color=#a29bfe>/marathon</color></b> — <b>speeds up ALL</b> players (increased speed) for <b>15s</b>, then back to normal.\n<b>Usage:</b> /marathon (no argument). <i>In game only. Cooldown 30s.</i>",
            "<b><color=#a29bfe>/marathon</color></b> — <b>accélère TOUS</b> les joueurs (vitesse augmentée) pendant <b>15s</b>, puis retour à la normale.\n<b>Écriture :</b> /marathon (aucun argument). <i>En jeu uniquement. Cooldown 30s.</i>");
        public static string HMarathonPlain => Localization.Pick(
            "/marathon - speeds up everyone 15s. Usage: /marathon. In game. Cooldown 30s.",
            "/marathon - accelere tout le monde 15s. Ecriture : /marathon. En jeu. Cooldown 30s.");
        public static string HRoulette => Localization.Pick(
            "<b><color=#ff6b6b>/roulette</color></b> — eliminates a <b>random</b> living player, after a short suspense.\n<b>Usage:</b> /roulette (no argument). <i>In game only. Cooldown 45s.</i>",
            "<b><color=#ff6b6b>/roulette</color></b> — élimine un joueur vivant <b>au hasard</b>, après un court suspense.\n<b>Écriture :</b> /roulette (aucun argument). <i>En jeu uniquement. Cooldown 45s.</i>");
        public static string HRoulettePlain => Localization.Pick(
            "/roulette - eliminates a random living player. Usage: /roulette. In game. Cooldown 45s.",
            "/roulette - elimine un joueur vivant au hasard. Ecriture : /roulette. En jeu. Cooldown 45s.");
        public static string HBodyswap => Localization.Pick(
            "<b><color=#a29bfe>/bodyswap IDA IDB</color></b> — swaps the <b>identity</b> (color + name) of two players: total confusion.\n<b>Usage:</b> /bodyswap A B. <i>In game only. Cooldown 30s.</i>",
            "<b><color=#a29bfe>/bodyswap IDA IDB</color></b> — échange l'<b>identité</b> (couleur + pseudo) de deux joueurs : confusion totale.\n<b>Écriture :</b> /bodyswap A B. <i>En jeu uniquement. Cooldown 30s.</i>");
        public static string HBodyswapPlain => Localization.Pick(
            "/bodyswap IDA IDB - swaps color + name of two players. Usage: /bodyswap A B. In game. Cooldown 30s.",
            "/bodyswap IDA IDB - echange couleur + pseudo de deux joueurs. Ecriture : /bodyswap A B. En jeu. Cooldown 30s.");

        public static string HStalker => Localization.Pick(
            "<b><color=#ffd23f>/stalker IDA IDB</color></b> — the <b>Obsessed</b>: player <b>A must stay within 3m of B</b> the whole round (both are warned privately). 10s grace at the start of the round, then if A strays too long, <b>A (the follower)</b> is eliminated.\n<b>Usage:</b> /stalker A B (A follows B). <i>During a meeting; the effect starts next round.</i>",
            "<b><color=#ffd23f>/stalker IDA IDB</color></b> — l'<b>Obsessionnel</b> : le joueur <b>A doit rester à moins de 3m de B</b> toute la manche (les deux sont prévenus en privé). 10s de grâce au début de la manche, puis si A s'éloigne trop longtemps, <b>A (le suiveur)</b> est éliminé.\n<b>Écriture :</b> /stalker A B (A suit B). <i>En réunion ; l'effet démarre à la manche suivante.</i>");
        public static string HStalkerPlain => Localization.Pick(
            "/stalker IDA IDB - A must stay near B the whole round, otherwise A is eliminated. Usage: /stalker A B. During meeting.",
            "/stalker IDA IDB - A doit rester pres de B toute la manche, sinon A est elimine. Ecriture : /stalker A B. En reunion.");
        public static string HUltimatum => Localization.Pick(
            "<b><color=#ff4d4d>/ultimatum ID [seconds]</color></b> — applies an ultimatum to an <b>impostor</b>: they must <b>kill before the time runs out</b> (60s by default, or the given number of seconds, <b>30s minimum</b>). If they kill no one, their <b>role is revealed to everyone</b> (name in red) and an <b>emergency meeting</b> is triggered automatically.\n<b>Usage:</b> /ultimatum A  (60s) or /ultimatum A 90  (90s). <i>During a meeting; starts next round.</i>",
            "<b><color=#ff4d4d>/ultimatum ID [secondes]</color></b> — applique un ultimatum à un <b>imposteur</b> : il doit faire un <b>kill avant la fin du délai</b> (60s par défaut, ou le nombre de secondes indiqué, <b>30s minimum</b>). S'il ne tue personne, son <b>rôle est révélé à tous</b> (pseudo en rouge) et une <b>réunion d'urgence</b> se déclenche automatiquement.\n<b>Écriture :</b> /ultimatum A  (60s) ou /ultimatum A 90  (90s). <i>En réunion ; démarre à la manche suivante.</i>");
        public static string HUltimatumPlain => Localization.Pick(
            "/ultimatum ID [s] - an impostor must kill before the deadline (60s default, 30s minimum) otherwise revealed + meeting. Usage: /ultimatum A or /ultimatum A 90. During meeting.",
            "/ultimatum ID [s] - un imposteur doit tuer avant le delai (60s defaut, 30s minimum) sinon revele + meeting. Ecriture : /ultimatum A ou /ultimatum A 90. En reunion.");

        public static string HStart => Localization.Pick(
            "<b><color=#ff4d4d>/start</color></b> — starts the game from the lobby (= Start button). <i>Host only.</i>\n<b>Usage:</b> /start (in lobby).",
            "<b><color=#ff4d4d>/start</color></b> — lance la partie depuis le lobby (= bouton Démarrer). <i>Hôte uniquement.</i>\n<b>Écriture :</b> /start (en lobby).");
        public static string HStartPlain => Localization.Pick(
            "/start - starts the game (host, in lobby). Usage: /start.",
            "/start - lance la partie (hote, en lobby). Ecriture : /start.");
        public static string HStop => Localization.Pick(
            "<b><color=#ff4d4d>/stop</color></b> — immediately ends the current game. <i>Host only.</i>\n<b>Usage:</b> /stop (in game).",
            "<b><color=#ff4d4d>/stop</color></b> — termine immédiatement la partie en cours. <i>Hôte uniquement.</i>\n<b>Écriture :</b> /stop (en partie).");
        public static string HStopPlain => Localization.Pick(
            "/stop - ends the current game (host). Usage: /stop.",
            "/stop - termine la partie en cours (hote). Ecriture : /stop.");
        public static string HSetdirector => Localization.Pick(
            "<b><color=#ff4d4d>/setdirector [ID]</color></b> — designates the Director. With an ID: that player; without ID: yourself. <i>Host only.</i>\n<b>Usage:</b> /setdirector  or  /setdirector A.",
            "<b><color=#ff4d4d>/setdirector [ID]</color></b> — désigne le Réalisateur. Avec un ID : ce joueur ; sans ID : toi-même. <i>Hôte uniquement.</i>\n<b>Écriture :</b> /setdirector  ou  /setdirector A.");
        public static string HSetdirectorPlain => Localization.Pick(
            "/setdirector [ID] - designates the Director (you if no ID). Usage: /setdirector or /setdirector A.",
            "/setdirector [ID] - designe le Realisateur (toi si pas d'ID). Ecriture : /setdirector ou /setdirector A.");
        public static string HRename => Localization.Pick(
            "<b><color=#ff4d4d>/rename ID NEW_NAME</color></b> — renames a player (the name can contain spaces). <i>Host only.</i>\n<b>Usage:</b> /rename A Bob.",
            "<b><color=#ff4d4d>/rename ID NOUVEAU_NOM</color></b> — renomme un joueur (le nom peut contenir des espaces). <i>Hôte uniquement.</i>\n<b>Écriture :</b> /rename A Bob.");
        public static string HRenamePlain => Localization.Pick(
            "/rename ID NAME - renames a player (host). Usage: /rename A Bob.",
            "/rename ID NOM - renomme un joueur (hote). Ecriture : /rename A Bob.");
        public static string HKill => Localization.Pick(
            "<b><color=#ff4d4d>/kill ID</color></b> — immediately eliminates a player. <i>Host only, in game.</i>\n<b>Usage:</b> /kill A.",
            "<b><color=#ff4d4d>/kill ID</color></b> — élimine immédiatement un joueur. <i>Hôte uniquement, en partie.</i>\n<b>Écriture :</b> /kill A.");
        public static string HKillPlain => Localization.Pick(
            "/kill ID - eliminates a player (host, in game). Usage: /kill A.",
            "/kill ID - elimine un joueur (hote, en partie). Ecriture : /kill A.");
        public static string HKick => Localization.Pick(
            "<b><color=#ff4d4d>/kick ID</color></b> — removes a player from the lobby/game. <i>Host only.</i>\n<b>Usage:</b> /kick A.",
            "<b><color=#ff4d4d>/kick ID</color></b> — exclut un joueur du lobby/de la partie. <i>Hôte uniquement.</i>\n<b>Écriture :</b> /kick A.");
        public static string HKickPlain => Localization.Pick(
            "/kick ID - removes a player (host). Usage: /kick A.",
            "/kick ID - exclut un joueur (hote). Ecriture : /kick A.");
        public static string HEndmeeting => Localization.Pick(
            "<b><color=#ff4d4d>/endmeeting</color></b> — forces the end of the current meeting (closes the vote). <i>Host only.</i>\n<b>Usage:</b> /endmeeting (during a meeting).",
            "<b><color=#ff4d4d>/endmeeting</color></b> — force la fin de la réunion en cours (clôture le vote). <i>Hôte uniquement.</i>\n<b>Écriture :</b> /endmeeting (pendant une réunion).");
        public static string HEndmeetingPlain => Localization.Pick(
            "/endmeeting - forces the end of the meeting (host). Usage: /endmeeting.",
            "/endmeeting - force la fin de la reunion (hote). Ecriture : /endmeeting.");
        public static string HStatus => Localization.Pick(
            "<b><color=#ff4d4d>/status</color></b> — shows the currently ACTIVE effects and directives (darkness, colorblind, frozen, stalker, ultimatum…) + the Director. <i>Host only.</i>\n<b>Usage:</b> /status.",
            "<b><color=#ff4d4d>/status</color></b> — affiche les effets et directives actuellement ACTIFS (darkness, colorblind, gelés, stalker, ultimatum…) + le Réalisateur. <i>Hôte uniquement.</i>\n<b>Écriture :</b> /status.");
        public static string HStatusPlain => Localization.Pick(
            "/status - active effects and directives + Director (host). Usage: /status.",
            "/status - effets et directives actifs + Realisateur (hote). Ecriture : /status.");

        public static string HLang => Localization.Pick(
            "<b><color=#ffd23f>/lang en|fr</color></b> — changes YOUR own language (English or French). It only affects the player who runs it.\n<b>Usage:</b> /lang en  or  /lang fr. Everyone, everywhere.",
            "<b><color=#ffd23f>/lang en|fr</color></b> — change TA langue (anglais ou français). N'affecte que le joueur qui l'utilise.\n<b>Écriture :</b> /lang en  ou  /lang fr. Tout le monde, partout.");
        public static string HLangPlain => Localization.Pick(
            "/lang en|fr - changes your own language. Usage: /lang en or /lang fr.",
            "/lang en|fr - change ta langue. Ecriture : /lang en ou /lang fr.");
        public static string HLangForAll => Localization.Pick(
            "<b><color=#ff4d4d>/langforall en|fr</color></b> — changes the language for EVERYONE in the lobby. <i>Host only.</i>\n<b>Usage:</b> /langforall en  or  /langforall fr. Everywhere.",
            "<b><color=#ff4d4d>/langforall en|fr</color></b> — change la langue pour TOUT le monde dans le lobby. <i>Hôte uniquement.</i>\n<b>Écriture :</b> /langforall en  ou  /langforall fr. Partout.");
        public static string HLangForAllPlain => Localization.Pick(
            "/langforall en|fr - changes the language for everyone (host). Usage: /langforall en or /langforall fr.",
            "/langforall en|fr - change la langue pour tout le monde (hote). Ecriture : /langforall en ou /langforall fr.");

        public static string LangChanged => Localization.Pick(
            "<b><color=#00e676>Language</color></b> set to <b>English</b>.",
            "<b><color=#00e676>Langue</color></b> réglée sur <b>Français</b>.");
        public static string LangChangedPlain => Localization.Pick(
            "Language set to English.",
            "Langue reglee sur Francais.");
        public static string LangChangedAll => Localization.Pick(
            "<b><color=#00e676>Language</color></b> set to <b>English</b> for everyone in the lobby.",
            "<b><color=#00e676>Langue</color></b> réglée sur <b>Français</b> pour tout le monde dans le lobby.");
        public static string LangChangedAllPlain => Localization.Pick(
            "Language set to English for everyone in the lobby.",
            "Langue reglee sur Francais pour tout le monde dans le lobby.");
        public static string UsageLang => Localization.Pick(
            "<b>Usage:</b> /lang en  |  /lang fr",
            "<b>Usage :</b> /lang en  |  /lang fr");
        public static string UsageLangPlain => Localization.Pick(
            "Usage: /lang en | /lang fr",
            "Usage : /lang en | /lang fr");
        public static string UsageLangForAll => Localization.Pick(
            "<b>Usage:</b> /langforall en  |  /langforall fr",
            "<b>Usage :</b> /langforall en  |  /langforall fr");
        public static string UsageLangForAllPlain => Localization.Pick(
            "Usage: /langforall en | /langforall fr",
            "Usage : /langforall en | /langforall fr");

        public static string HelpAll => Localization.Pick(
            "<b><color=#ff6b6b>══ THE DIRECTOR'S CUT ══</color></b>\n" +
            "<b><color=#ffd23f>General</color></b>: /help · /welcome · /gg · /players · /discord · /cooldowns · /lang en|fr\n" +
            "<b><color=#ffd23f>Director</color></b>\n" +
            "/cut · /darkness · /freeze A · /randomcolors · /colorblind\n" +
            "/shuffle · /swap A B · /teleportall A · /tp A B\n" +
            "/voiceover txt · /spotlight A · /marathon\n" +
            "/roulette · /bodyswap A B\n" +
            "/action A X · /loc A Z · /vote A B <i>(meeting)</i>\n" +
            "/stalker A B · /ultimatum A [s] <i>(meeting)</i>\n" +
            "<b><color=#b2bec3>Details</color></b>: put <b>/h</b> before ANY command (ex: /hcut, /htp, /hultimatum, /hkick)",
            "<b><color=#ff6b6b>══ THE DIRECTOR'S CUT ══</color></b>\n" +
            "<b><color=#ffd23f>Général</color></b> : /help · /welcome · /gg · /players · /discord · /cooldowns · /lang en|fr\n" +
            "<b><color=#ffd23f>Réalisateur</color></b>\n" +
            "/cut · /darkness · /freeze A · /randomcolors · /colorblind\n" +
            "/shuffle · /swap A B · /teleportall A · /tp A B\n" +
            "/voiceover txt · /spotlight A · /marathon\n" +
            "/roulette · /bodyswap A B\n" +
            "/action A X · /loc A Z · /vote A B <i>(réunion)</i>\n" +
            "/stalker A B · /ultimatum A [s] <i>(réunion)</i>\n" +
            "<b><color=#b2bec3>Détails</color></b> : mets <b>/h</b> devant N'IMPORTE quelle commande (ex : /hcut, /htp, /hultimatum, /hkick)");
        public static string HelpAllPlain => Localization.Pick(
            "== THE DIRECTOR'S CUT ==\n" +
            "General: /help /welcome /gg /players /discord /cooldowns /lang en|fr\n" +
            "Director:\n" +
            "/cut /darkness /freeze A /randomcolors /colorblind\n" +
            "/shuffle /swap A B /teleportall A /tp A B\n" +
            "/voiceover txt /spotlight A /marathon\n" +
            "/roulette /bodyswap A B\n" +
            "/action A X /loc A Z /vote A B (meeting)\n" +
            "/stalker A B /ultimatum A [s] (meeting)\n" +
            "Details: put /h before any command (ex: /hcut, /htp, /hultimatum, /hkick)",
            "== THE DIRECTOR'S CUT ==\n" +
            "General : /help /welcome /gg /players /discord /cooldowns /lang en|fr\n" +
            "Realisateur :\n" +
            "/cut /darkness /freeze A /randomcolors /colorblind\n" +
            "/shuffle /swap A B /teleportall A /tp A B\n" +
            "/voiceover txt /spotlight A /marathon\n" +
            "/roulette /bodyswap A B\n" +
            "/action A X /loc A Z /vote A B (reunion)\n" +
            "/stalker A B /ultimatum A [s] (reunion)\n" +
            "Details : mets /h devant n'importe quelle commande (ex : /hcut, /htp, /hultimatum, /hkick)");

        public static string HelpAdminLine => Localization.Pick(
            "<b><color=#ff4d4d>Admin (host)</color></b>: /start · /stop · /setdirector A · /rename A name · /kill A · /kick A · /endmeeting · /status · /langforall en|fr · <i>[Del] = panel</i>",
            "<b><color=#ff4d4d>Admin (hôte)</color></b> : /start · /stop · /setdirector A · /rename A nom · /kill A · /kick A · /endmeeting · /status · /langforall en|fr · <i>[Suppr] = panneau</i>");
        public static string HelpAdminLinePlain => Localization.Pick(
            "Admin (host): /start /stop /setdirector A /rename A name /kill A /kick A /endmeeting /status /langforall en|fr [Del] = panel",
            "Admin (hote) : /start /stop /setdirector A /rename A nom /kill A /kick A /endmeeting /status /langforall en|fr [Suppr] = panneau");

        public static string HelpAdmin => Localization.Pick(
            "<b><color=#ff4d4d>Admin (host)</color></b>\n" +
            "/start — starts the game  •  /stop — stops the game\n" +
            "/setdirector [ID] — designates the Director\n" +
            "/rename ID NAME — renames a player\n" +
            "/kill ID — eliminates a player\n" +
            "/endmeeting — ends the current meeting\n" +
            "<i>(Del / Delete key: opens the Admin panel)</i>",
            "<b><color=#ff4d4d>Admin (hôte)</color></b>\n" +
            "/start — lance la partie  •  /stop — arrête la partie\n" +
            "/setdirector [ID] — désigne le Réalisateur\n" +
            "/rename ID NOM — renomme un joueur\n" +
            "/kill ID — élimine un joueur\n" +
            "/endmeeting — termine la réunion en cours\n" +
            "<i>(touche Suppr / Delete : ouvre le panneau Admin)</i>");
        public static string HelpAdminPlain => Localization.Pick(
            "Admin (host)\n" +
            "/start - starts the game  -  /stop - stops the game\n" +
            "/setdirector [ID] - designates the Director\n" +
            "/rename ID NAME - renames a player\n" +
            "/kill ID - eliminates a player\n" +
            "/endmeeting - ends the current meeting\n" +
            "(Del / Delete key: opens the Admin panel)",
            "Admin (hote)\n" +
            "/start - lance la partie  -  /stop - arrete la partie\n" +
            "/setdirector [ID] - designe le Realisateur\n" +
            "/rename ID NOM - renomme un joueur\n" +
            "/kill ID - elimine un joueur\n" +
            "/endmeeting - termine la reunion en cours\n" +
            "(touche Suppr / Delete : ouvre le panneau Admin)");

        public static string CutStart => Localization.Pick(
            "<b><color=#ff6b6b>CUT!</color></b> Reactor sabotage (2s) → <b>STOP</b> (5s):\n<b>DON'T MOVE</b> — everyone who moves is eliminated!",
            "<b><color=#ff6b6b>CUT !</color></b> Sabotage réacteur (2s) → <b>ARRÊT</b> (5s) :\n<b>NE BOUGEZ PLUS</b> — tous les bougeurs sont éliminés !");
        public static string CutStartPlain => Localization.Pick(
            "CUT! Reactor sabotage (2s) -> STOP (5s): DON'T MOVE, everyone who moves is eliminated!",
            "CUT ! Sabotage reacteur (2s) -> ARRET (5s) : NE BOUGEZ PLUS, tous les bougeurs sont elimines !");

        public static string CutEliminated => Localization.Pick(
            "<b><color=#ff6b6b>{0}</color></b> moved — <b>eliminated!</b>",
            "<b><color=#ff6b6b>{0}</color></b> a bougé — <b>éliminé !</b>");
        public static string CutEliminatedPlain => Localization.Pick(
            "{0} moved - eliminated!",
            "{0} a bouge - elimine !");

        public static string DarknessStart => Localization.Pick(
            "<b><color=#2d3436>DARKNESS!</color></b> TOTAL BLACKOUT for 10s!",
            "<b><color=#2d3436>DARKNESS !</color></b> NOIR TOTAL pendant 10s !");
        public static string DarknessStartPlain => Localization.Pick(
            "DARKNESS! TOTAL BLACKOUT for 10s!",
            "DARKNESS ! NOIR TOTAL pendant 10s !");

        public static string DarknessEnd => Localization.Pick(
            "<b><color=#ffd23f>LIGHT!</color></b> Back to normal!",
            "<b><color=#ffd23f>LUMIÈRE !</color></b> Retour à la normale !");
        public static string DarknessEndPlain => Localization.Pick(
            "LIGHT! Back to normal!",
            "LUMIERE ! Retour a la normale !");

        public static string FreezeStart => Localization.Pick(
            "<b><color=#74b9ff>FREEZE!</color></b> <b>{0}</b> is frozen for 8s!",
            "<b><color=#74b9ff>FREEZE !</color></b> <b>{0}</b> est bloqué 8s !");
        public static string FreezeStartPlain => Localization.Pick(
            "FREEZE! {0} is frozen for 8s!",
            "FREEZE ! {0} est bloque 8s !");

        public static string FreezeEnd => Localization.Pick(
            "<b><color=#00e676>GO!</color></b> <b>{0}</b> can move again!",
            "<b><color=#00e676>GO !</color></b> <b>{0}</b> peut à nouveau bouger !");
        public static string FreezeEndPlain => Localization.Pick(
            "GO! {0} can move again!",
            "GO ! {0} peut a nouveau bouger !");

        public static string ColorBlindStart => Localization.Pick(
            "<b><color=#b2bec3>COLORBLIND!</color></b> Everyone gray, names hidden (25s)!",
            "<b><color=#b2bec3>COLORBLIND !</color></b> Tout le monde en gris, noms masqués (25s) !");
        public static string ColorBlindStartPlain => Localization.Pick(
            "COLORBLIND! Everyone gray, names hidden (25s)!",
            "COLORBLIND ! Tout le monde en gris, noms masques (25s) !");

        public static string ColorBlindEnd => Localization.Pick(
            "<b><color=#ffd23f>BACK!</color></b> Colors and names restored!",
            "<b><color=#ffd23f>RETOUR !</color></b> Couleurs et noms rétablis !");
        public static string ColorBlindEndPlain => Localization.Pick(
            "BACK! Colors and names restored!",
            "RETOUR ! Couleurs et noms retablis !");

        public static string ShuffleStart => Localization.Pick(
            "<b><color=#a29bfe>SHUFFLE!</color></b> Positions randomly shuffled!",
            "<b><color=#a29bfe>SHUFFLE !</color></b> Positions mélangées au hasard !");
        public static string ShuffleStartPlain => Localization.Pick(
            "SHUFFLE! Positions randomly shuffled!",
            "SHUFFLE ! Positions melangees au hasard !");

        public static string SwapDone => Localization.Pick(
            "<b><color=#a29bfe>SWAP!</color></b> <b>{0}</b> and <b>{1}</b> swapped positions!",
            "<b><color=#a29bfe>SWAP !</color></b> <b>{0}</b> et <b>{1}</b> ont échangé leurs positions !");
        public static string SwapDonePlain => Localization.Pick(
            "SWAP! {0} and {1} swapped positions!",
            "SWAP ! {0} et {1} ont echange leurs positions !");

        public static string TeleportAllDone => Localization.Pick(
            "<b><color=#a29bfe>TELEPORT!</color></b> Everyone was teleported to <b>{0}</b>!",
            "<b><color=#a29bfe>TÉLÉPORT !</color></b> Tout le monde a été téléporté vers <b>{0}</b> !");
        public static string TeleportAllDonePlain => Localization.Pick(
            "TELEPORT! Everyone was teleported to {0}!",
            "TELEPORT ! Tout le monde a ete teleporte vers {0} !");
    }
}
