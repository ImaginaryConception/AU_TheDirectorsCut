namespace AU_TheDirectorsCut
{
    public static class DirectorOptions
    {
        public static bool  AnnounceInChat = true;   // relayer les actions dans le chat public
        public static bool  AntiKick       = true;   // throttle anti-kick (garder activé)
        public static bool  CutKills       = true;   // /cut élimine les bougeurs
        public static float MessageWait    = 0.6f;   // délai entre deux messages (secondes)
    }
}
