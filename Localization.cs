using System.Collections.Generic;

namespace AU_TheDirectorsCut
{
    public enum Lang { EN, FR }

    public static class Localization
    {
        public static Lang DefaultLang = Lang.EN;


        private static readonly Dictionary<byte, Lang> _playerLang = new();

        public static Lang CurrentLang = Lang.EN;

        public static Lang Get(byte playerId)
            => _playerLang.TryGetValue(playerId, out var l) ? l : DefaultLang;

        public static void SetPlayer(byte playerId, Lang lang)
            => _playerLang[playerId] = lang;

        public static void SetForAll(Lang lang)
        {
            DefaultLang = lang;
            _playerLang.Clear();
        }

        public static void ClearOverrides() => _playerLang.Clear();

        public static bool TryParse(string s, out Lang lang)
        {
            lang = DefaultLang;
            if (string.IsNullOrWhiteSpace(s)) return false;
            switch (s.Trim().ToLowerInvariant())
            {
                case "en":
                case "eng":
                case "english":
                case "anglais":
                    lang = Lang.EN;
                    return true;
                case "fr":
                case "fra":
                case "french":
                case "francais":
                case "français":
                    lang = Lang.FR;
                    return true;
                default:
                    return false;
            }
        }

        public static string Pick(string en, string fr) => CurrentLang == Lang.EN ? en : fr;

        public static string Tr(Lang lang, string en, string fr) => lang == Lang.EN ? en : fr;
    }
}
