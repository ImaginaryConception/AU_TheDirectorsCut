using BepInEx.Configuration;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    // Configuration éditable sans recompiler (BepInEx/config/com.anish.au.thedirectorscut.cfg).
    public static class ModConfig
    {
        public static ConfigEntry<KeyCode> AdminPanelKey;
        public static ConfigEntry<bool> BotCosmetics;
        public static ConfigEntry<string> DiscordLink;

        public static void Init(ConfigFile config)
        {
            AdminPanelKey = config.Bind(
                "Général", "AdminPanelKey", KeyCode.Delete,
                "Touche qui ouvre/ferme le panneau Admin (hôte uniquement).");

            BotCosmetics = config.Bind(
                "Général", "BotCosmetics", true,
                "Donne au bot un pseudo bleu ET une couleur d'avatar distincte. Mettre false pour ne garder que le pseudo bleu.");

            DiscordLink = config.Bind(
                "Général", "DiscordLink", "",
                "Lien d'invitation Discord affiché par /discord. Laisser vide pour afficher les contacts par défaut.");
        }
    }
}
