namespace ZeroLevel.Services.Serialization
{
    internal static class JsonEscaper
    {
        /// <summary>
        /// Checking if needed escaping string for use as json value
        /// </summary>
        /// <param name="src"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool NeedEscape(string src, int i)
        {
            if (string.IsNullOrWhiteSpace(src)) return false;
            char c = src[i];
            return c < 32 || c == '"' || c == '\\'
                // Broken lead surrogate
                || (c >= '\uD800' && c <= '\uDBFF' &&
                    (i == src.Length - 1 || src[i + 1] < '\uDC00' || src[i + 1] > '\uDFFF'))
                // Broken tail surrogate
                || (c >= '\uDC00' && c <= '\uDFFF' &&
                    (i == 0 || src[i - 1] < '\uD800' || src[i - 1] > '\uDBFF'))
                // To produce valid JavaScript
                || c == '\u2028' || c == '\u2029'
                // Escape "</" for <script> tags
                || (c == '/' && i > 0 && src[i - 1] == '<');
        }

        /// <summary>
        /// Escape string for use as json value
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string EscapeString(string src)
        {
            if (string.IsNullOrWhiteSpace(src)) return src;
            var sb = new System.Text.StringBuilder();
            int start = 0;
            for (int i = 0; i < src.Length; i++)
                if (NeedEscape(src, i))
                {
                    sb.Append(src, start, i - start);
                    switch (src[i])
                    {
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        default:
                            sb.Append("\\u");
                            sb.Append(((int)src[i]).ToString("x04"));
                            break;
                    }
                    start = i + 1;
                }
            sb.Append(src, start, src.Length - start);
            return sb.ToString();
        }
    }
}