using BepInEx.Configuration;
using UnityEngine;

namespace AU_TheDirectorsCut
{
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
                "Général", "DiscordLink", "https://discord.gg/X58Z2dNZ96",
                "Lien d'invitation Discord affiché par /discord et dans le message de bienvenue. Laisser vide pour afficher les contacts par défaut.");
        }
    }
}
