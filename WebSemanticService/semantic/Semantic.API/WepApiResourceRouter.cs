using System.Collections.Generic;

namespace ZeroLevel.WebAPI
{
    public static class WepApiResourceRouter
    {
        private static readonly Dictionary<string, string> _javaScriptResources =
            new Dictionary<string, string>();

        private static readonly Dictionary<string, string> _cssScriptResources =
            new Dictionary<string, string>();

        private static readonly Dictionary<string, string> _htmlResources =
            new Dictionary<string, string>();

        public static void RegisterJavaScriptFile(string resourceName, string fileName)
        {
            var key = resourceName.ToLowerInvariant();
            if (false == _javaScriptResources.ContainsKey(key))
            {
                _javaScriptResources.Add(key, fileName);
            }
            else
            {
                _javaScriptResources[key] = fileName;
            }
        }

        public static void RegisterCSSFile(string resourceName, string fileName)
        {
            var key = resourceName.ToLowerInvariant();
            if (false == _cssScriptResources.ContainsKey(key))
            {
                _cssScriptResources.Add(key, fileName);
            }
            else
            {
                _cssScriptResources[key] = fileName;
            }
        }

        public static void RegisterHTMLFile(string resourceName, string fileName)
        {
            var key = resourceName.ToLowerInvariant();
            if (false == _htmlResources.ContainsKey(key))
            {
                _htmlResources.Add(key, fileName);
            }
            else
            {
                _htmlResources[key] = fileName;
            }
        }

        public static string GetJsFile(string resourceName)
        {
            var key = resourceName.ToLowerInvariant();
            if (true == _javaScriptResources.ContainsKey(key))
            {
                return _javaScriptResources[key];
            }
            return null;
        }

        public static string GetCssFile(string resourceName)
        {
            var key = resourceName.ToLowerInvariant();
            if (true == _cssScriptResources.ContainsKey(key))
            {
                return _cssScriptResources[key];
            }
            return null;
        }

        public static string GetHtmlFile(string resourceName)
        {
            var key = resourceName.ToLowerInvariant();
            if (true == _htmlResources.ContainsKey(key))
            {
                return _htmlResources[key];
            }
            return null;
        }
    }
}
