using System;

namespace DOM.Services
{
    public static class LanguageParser
    {
        public const string RUS = "ru";
        public const string ENG = "en";

        /// <summary>
        /// Detect language by abbreviation
        /// </summary>
        /// <param name="lang">Abbreviation</param>
        /// <returns>Language</returns>
        public static string Parse(string lang)
        {
            if (false == string.IsNullOrWhiteSpace(lang))
            {
                var key = lang.Trim().ToLowerInvariant();
                if (key.IndexOf("ru", StringComparison.InvariantCulture) >= 0)
                {
                    return RUS;
                }
                if (key.IndexOf("en", StringComparison.InvariantCulture) >= 0)
                {
                    return ENG;
                }
            }
            return RUS;
        }
    }
}