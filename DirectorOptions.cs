namespace AU_TheDirectorsCut
{
    // ---------------------------------------------------------------------
    //  Options activables/désactivables depuis l'UI (cases à cocher).
    //  Lues par ChatManager (et par ton DirectorCore si tu branches les
    //  one-liners indiqués dans les notes).
    // ---------------------------------------------------------------------
    public static class DirectorOptions
    {
        // Relayer les retours d'action (/cut, /swap...) dans le chat public.
        // Nécessite la petite modif de SendHostMessage (voir notes).
        public static bool AnnounceInChat = true;

        // Throttle anti-kick sur l'envoi des messages chat. À garder ON.
        public static bool AntiKick = true;

        // /cut élimine les joueurs qui bougent (sinon : avertissement seul).
        // Nécessite de garder le test dans CheckCutMovement (voir notes).
        public static bool CutKills = true;

        // Délai minimum entre deux messages (secondes) quand AntiKick est ON.
        public static float MessageWait = 0.6f;
    }
}
