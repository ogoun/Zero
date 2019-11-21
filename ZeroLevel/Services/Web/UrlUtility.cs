using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroLevel.Services.Web
{
    public static class UrlUtility
    {
        //  Query string parsing support
        public static IDictionary<string, string> ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static IDictionary<string, string> ParseQueryString(string query, Encoding encoding)
        {
            if (query == null)
            {
                return null;
            }
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }
            if (query.Length > 0 && query[0] == '?')
            {
                query = query.Substring(1);
            }
            return CreateCollection(query, true, encoding);
        }

        public static string UrlEncode(string str)
        {
            return str == null ? null : UrlEncode(str, Encoding.UTF8);
        }

        // URL encodes a path portion of a URL string and returns the encoded string.
        public static string UrlPathEncode(string str)
        {
            if (str == null)
            {
                return null;
            }
            // recurse in case there is a query string
            var i = str.IndexOf('?');
            if (i >= 0)
            {
                return UrlPathEncode(str.Substring(0, i)) + str.Substring(i);
            }
            // encode DBCS characters and spaces only
            return UrlEncodeSpaces(UrlEncodeNonAscii(str, Encoding.UTF8));
        }

        public static string UrlEncode(string str, Encoding encoding)
        {
            return str == null ? null : Encoding.ASCII.GetString(UrlEncodeToBytes(str, encoding));
        }

        public static string UrlEncodeUnicode(string str)
        {
            return str == null ? null : UrlEncodeUnicodeStringToStringInternal(str, false);
        }

        /// <summary>
        /// https://github.com/tmenier/Flurl
        /// </summary>
        public static string Combine(params string[] parts)
        {
            if (parts == null)
                throw new ArgumentNullException(nameof(parts));

            string result = "";
            bool inQuery = false, inFragment = false;

            string CombineEnsureSingleSeparator(string a, string b, char separator)
            {
                if (string.IsNullOrEmpty(a)) return b;
                if (string.IsNullOrEmpty(b)) return a;
                return a.TrimEnd(separator) + separator + b.TrimStart(separator);
            }

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                if (result.EndsWith("?") || part.StartsWith("?"))
                    result = CombineEnsureSingleSeparator(result, part, '?');
                else if (result.EndsWith("#") || part.StartsWith("#"))
                    result = CombineEnsureSingleSeparator(result, part, '#');
                else if (inFragment)
                    result += part;
                else if (inQuery)
                    result = CombineEnsureSingleSeparator(result, part, '&');
                else
                    result = CombineEnsureSingleSeparator(result, part, '/');

                if (part.Contains("#"))
                {
                    inQuery = false;
                    inFragment = true;
                }
                else if (!inFragment && part.Contains("?"))
                {
                    inQuery = true;
                }
            }
            return EncodeIllegalCharacters(result);
        }
        /// <summary>
        /// https://github.com/tmenier/Flurl
        /// </summary>
        public static string EncodeIllegalCharacters(string s, bool encodeSpaceAsPlus = false)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (encodeSpaceAsPlus)
                s = s.Replace(" ", "+");

            // Uri.EscapeUriString mostly does what we want - encodes illegal characters only - but it has a quirk
            // in that % isn't illegal if it's the start of a %-encoded sequence https://stackoverflow.com/a/47636037/62600

            // no % characters, so avoid the regex overhead
            if (!s.Contains("%"))
                return Uri.EscapeUriString(s);

            // pick out all %-hex-hex matches and avoid double-encoding 
            return Regex.Replace(s, "(.*?)((%[0-9A-Fa-f]{2})|$)", c => {
                var a = c.Groups[1].Value; // group 1 is a sequence with no %-encoding - encode illegal characters
                var b = c.Groups[2].Value; // group 2 is a valid 3-character %-encoded sequence - leave it alone!
                return Uri.EscapeUriString(a) + b;
            });
        }

        private static string UrlEncodeUnicodeStringToStringInternal(string s, bool ignoreAscii)
        {
            var l = s.Length;
            var sb = new StringBuilder(l);

            for (var i = 0; i < l; i++)
            {
                var ch = s[i];

                if ((ch & 0xff80) == 0)
                {  // 7 bit?
                    if (ignoreAscii || IsSafe(ch))
                    {
                        sb.Append(ch);
                    }
                    else if (ch == ' ')
                    {
                        sb.Append('+');
                    }
                    else
                    {
                        sb.Append('%');
                        sb.Append(IntToHex((ch >> 4) & 0xf));
                        sb.Append(IntToHex((ch) & 0xf));
                    }
                }
                else
                { // arbitrary Unicode?
                    sb.Append("%u");
                    sb.Append(IntToHex((ch >> 12) & 0xf));
                    sb.Append(IntToHex((ch >> 8) & 0xf));
                    sb.Append(IntToHex((ch >> 4) & 0xf));
                    sb.Append(IntToHex((ch) & 0xf));
                }
            }
            return sb.ToString();
        }

        //  Helper to encode the non-ASCII url characters only
        private static string UrlEncodeNonAscii(string str, Encoding e)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            if (e == null)
            {
                e = Encoding.UTF8;
            }
            var bytes = e.GetBytes(str);
            bytes = UrlEncodeBytesToBytesInternalNonAscii(bytes, 0, bytes.Length, false);
            return Encoding.ASCII.GetString(bytes);
        }

        //  Helper to encode spaces only
        private static string UrlEncodeSpaces(string str)
        {
            if (str != null && str.IndexOf(' ') >= 0)
            {
                str = str.Replace(" ", "%20");
            }
            return str;
        }

        public static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            var bytes = e.GetBytes(str);
            return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
        }

        public static string UrlDecode(string str, Encoding e)
        {
            return str == null ? null : UrlDecodeStringFromStringInternal(str, e);
        }

        //  Implementation for encoding
        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
        {
            var cSpaces = 0;
            var cUnsafe = 0;

            // count them first
            for (var i = 0; i < count; i++)
            {
                var ch = (char)bytes[offset + i];
                if (ch == ' ')
                {
                    cSpaces++;
                }
                else if (!IsSafe(ch))
                {
                    cUnsafe++;
                }
            }

            // nothing to expand?
            if (!alwaysCreateReturnValue && cSpaces == 0 && cUnsafe == 0)
            {
                return bytes;
            }

            // expand not 'safe' characters into %XX, spaces to +s
            var expandedBytes = new byte[count + cUnsafe * 2];
            var pos = 0;

            for (var i = 0; i < count; i++)
            {
                var b = bytes[offset + i];
                var ch = (char)b;

                if (IsSafe(ch))
                {
                    expandedBytes[pos++] = b;
                }
                else if (ch == ' ')
                {
                    expandedBytes[pos++] = (byte)'+';
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            }
            return expandedBytes;
        }

        private static bool IsNonAsciiByte(byte b)
        {
            return (b >= 0x7F || b < 0x20);
        }

        private static byte[] UrlEncodeBytesToBytesInternalNonAscii(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
        {
            var cNonAscii = 0;

            // count them first
            for (var i = 0; i < count; i++)
            {
                if (IsNonAsciiByte(bytes[offset + i]))
                {
                    cNonAscii++;
                }
            }

            // nothing to expand?
            if (!alwaysCreateReturnValue && cNonAscii == 0)
            {
                return bytes;
            }
            // expand not 'safe' characters into %XX, spaces to +s
            var expandedBytes = new byte[count + cNonAscii * 2];
            var pos = 0;

            for (var i = 0; i < count; i++)
            {
                var b = bytes[offset + i];

                if (IsNonAsciiByte(b))
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
                else
                {
                    expandedBytes[pos++] = b;
                }
            }
            return expandedBytes;
        }

        private static string UrlDecodeStringFromStringInternal(string s, Encoding e)
        {
            var count = s.Length;
            var helper = new UrlDecoder(count, e);
            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes
            for (var pos = 0; pos < count; pos++)
            {
                var ch = s[pos];
                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    if (s[pos + 1] == 'u' && pos < count - 5)
                    {
                        var h1 = HexToInt(s[pos + 2]);
                        var h2 = HexToInt(s[pos + 3]);
                        var h3 = HexToInt(s[pos + 4]);
                        var h4 = HexToInt(s[pos + 5]);

                        if (h1 < 0 || h2 < 0 || h3 < 0 || h4 < 0)
                            continue; // valid 4 hex chars
                        ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                        pos += 5;

                        // only add as char
                        helper.AddChar(ch);
                        continue;
                    }
                    else
                    {
                        var h1 = HexToInt(s[pos + 1]);
                        var h2 = HexToInt(s[pos + 2]);
                        if (h1 < 0 || h2 < 0)
                            continue; // valid 2 hex chars
                        var b = (byte)((h1 << 4) | h2);
                        pos += 2;
                        // don't add as char
                        helper.AddByte(b);
                        continue;
                    }
                }
                if ((ch & 0xFF80) == 0)
                {
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                }
                else
                {
                    helper.AddChar(ch);
                }
            }
            return helper.GetString();
        }

        // Private helpers for URL encoding/decoding
        private static int HexToInt(char h)
        {
            return (h >= '0' && h <= '9') ? h - '0' :
            (h >= 'a' && h <= 'f') ? h - 'a' + 10 :
            (h >= 'A' && h <= 'F') ? h - 'A' + 10 :
            -1;
        }

        private static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + '0');
            }
            return (char)(n - 10 + 'a');
        }

        // Set of safe chars, from RFC 1738.4 minus '+'
        internal static bool IsSafe(char ch)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
            {
                return true;
            }
            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '\'':
                case '(':
                case ')':
                    return true;
            }
            return false;
        }

        // Internal class to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private class UrlDecoder
        {
            private readonly int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;

            private readonly char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;

            private byte[] _byteBuffer;

            // Encoding to convert chars to bytes
            private readonly Encoding _encoding;

            private void FlushBytes()
            {
                if (_numBytes <= 0) return;
                _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                _numBytes = 0;
            }

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;
                _charBuffer = new char[bufferSize];
                // byte buffer created on demand
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte(byte b)
            {
                if (_byteBuffer == null)
                {
                    _byteBuffer = new byte[_bufferSize];
                }

                _byteBuffer[_numBytes++] = b;
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                return _numChars > 0 ? new string(_charBuffer, 0, _numChars) : string.Empty;
            }
        }

        private static IDictionary<string, string> CreateCollection(string s, bool urlencoded, Encoding encoding)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(s))
                return result;
            var i = 0;
            var l = s.Length;
            while (i < l)
            {
                // find next & while noting first = on the way (and if there are more)
                var si = i;
                var ti = -1;
                while (i < l)
                {
                    var ch = s[i];
                    if (ch == '=')
                    {
                        if (ti < 0)
                            ti = i;
                    }
                    else if (ch == '&')
                    {
                        break;
                    }
                    i++;
                }
                // extract the name / value pair
                string name = null;
                string value = null;
                if (ti >= 0)
                {
                    name = s.Substring(si, ti - si);
                    value = s.Substring(ti + 1, i - ti - 1);
                }
                else
                {
                    name = s.Substring(si, i - si);
                }
                // add name / value pair to the collection
                if (urlencoded)
                {
                    result.Add(
                        UrlDecode(name, encoding).ToLower(),
                        UrlDecode(value, encoding));
                }
                else
                {
                    result.Add(name.ToLower(), value);
                }
                i++;
            }
            return result;
        }        
    }
}