using System.Collections.Generic;
using System.Text;
using ZeroLevel.Services.Text;

namespace ZeroLevel.Services.Web
{
    public static class HtmlUtility
    {
        #region Helpers

        private class SymToken
        {
            public readonly char Value;
            public readonly SymToken Preview;
            private SymToken _lazyNext = null!;
            private readonly int _index;
            private readonly string _line;

            public SymToken Next
            {
                get
                {
                    if (_line.Length == 0 || _index == _line.Length - 1)
                    {
                        return null!;
                    }
                    else
                    {
                        if (_lazyNext == null!)
                        {
                            _lazyNext = new SymToken(_line, _index + 1, this);
                        }
                    }
                    return _lazyNext;
                }
            }

            public SymToken(string line, int index, SymToken preview)
            {
                _index = index;
                _line = line;
                if (_line.Length > _index)
                {
                    Value = _line[_index];
                }
                else
                {
                    Value = char.MinValue;
                }
                Preview = preview;
            }

            public bool Test(string testLine)
            {
                var cursor = this;
                for (int i = 0; i < testLine.Length; i++)
                {
                    if (char.ToLowerInvariant(testLine[i]) != cursor.Value) return false;
                    cursor = cursor.Next;
                }
                return true;
            }
        }

        private class StringCursor
        {
            public SymToken Token;

            public StringCursor(string line)
            {
                if (false == string.IsNullOrEmpty(line))
                {
                    Token = new SymToken(line, 0, null!);
                }
                else
                {
                    Token = null!;
                }
            }

            public char Preview
            {
                get
                {
                    if (Token.Preview != null!)
                    {
                        return Token.Preview.Value;
                    }
                    return char.MinValue;
                }
            }

            public char Current
            {
                get
                {
                    if (Token != null!)
                    {
                        return Token.Value;
                    }
                    return char.MinValue;
                }
            }

            public char Next
            {
                get
                {
                    if (Token.Next != null!)
                    {
                        return Token.Next.Value;
                    }
                    return char.MinValue;
                }
            }

            public bool Test(string testLine)
            {
                return Token.Test(testLine);
            }

            public void MoveAfter(string testLine)
            {
                for (int i = 0; i < testLine.Length; i++)
                {
                    if (char.ToLowerInvariant(testLine[i]) == Token.Value)
                        Token = Token.Next;
                }
            }

            public bool MoveNext()
            {
                if (Token != null!)
                {
                    Token = Token.Next;
                    return true;
                }
                return false;
            }

            public bool EOF
            {
                get
                {
                    return Token == null!;
                }
            }
        }

        #endregion Helpers

        #region Mapping

        private readonly static Dictionary<string, string> _entityMap = new Dictionary<string, string>
        {
            {"quot;", "\""},    {"apos;", "'"},     {"amp;", "&"},      {"lt;", "<"},
            {"gt;", ">"},       {"nbsp;", ""},      {"iexcl;", "¡"},    {"cent;", "¢"},
            {"pound;", "£"},    {"curren;", "¤"},   {"yen;", "¥"},      {"brvbar;", "¦"},
            {"sect;", "§"},     {"uml;", "¨"},      {"copy;", "©"},     {"ordf;", "ª"},
            {"laquo;", "«"},    {"not;", "¬"},      {"shy;", "­"},       {"reg;", "®"},
            {"macr;", "¯"},     {"deg;", "°"},      {"plusmn;", "±"},   {"sup2;", "²"},
            {"sup3;", "³"},     {"acute;", "´"},    {"micro;", "µ"},    {"para;", "¶"},
            {"middot;", "·"},   {"cedil;", "¸"},    {"sup1;", "¹"},     {"ordm;", "º"},
            {"raquo;", "»"},    {"frac14;", "¼"},   {"frac12;", "½"},   {"frac34;", "¾"},
            {"iquest;", "¿"},   {"times;", "×"},    {"divide;", "÷"},   {"Agrave;", "À"},
            {"Aacute;", "Á"},   {"Acirc;", "Â"},    {"Atilde;", "Ã"},   {"Auml;", "Ä"},
            {"Aring;", "Å"},    {"AElig;", "Æ"},    {"Ccedil;", "Ç"},   {"Egrave;", "È"},
            {"Eacute;", "É"},   {"Ecirc;", "Ê"},    {"Euml;", "Ë"},     {"Igrave;", "Ì"},
            {"Iacute;", "Í"},   {"Icirc;", "Î"},    {"Iuml;", "Ï"},     {"ETH;", "Ð"},
            {"Ntilde;", "Ñ"},   {"Ograve;", "Ò"},   {"Oacute;", "Ó"},   {"Ocirc;", "Ô"},
            {"Otilde;", "Õ"},   {"Ouml;", "Ö"},     {"Oslash;", "Ø"},   {"Ugrave;", "Ù"},
            {"Uacute;", "Ú"},   {"Ucirc;", "Û"},    {"Uuml;", "Ü"},     {"Yacute;", "Ý"},
            {"THORN;", "Þ"},    {"szlig;", "ß"},    {"agrave;", "à"},   {"aacute;", "á"},
            {"acirc;", "â"},    {"atilde;", "ã"},   {"auml;", "ä"},     {"aring;", "å"},
            {"aelig;", "æ"},    {"ccedil;", "ç"},   {"egrave;", "è"},   {"eacute;", "é"},
            {"ecirc;", "ê"},    {"euml;", "ë"},     {"igrave;", "ì"},   {"iacute;", "í"},
            {"icirc;", "î"},    {"iuml;", "ï"},     {"eth;", "ð"},      {"ntilde;", "ñ"},
            {"ograve;", "ò"},   {"oacute;", "ó"},   {"ocirc;", "ô"},    {"otilde;", "õ"},
            {"ouml;", "ö"},     {"oslash;", "ø"},   {"ugrave;", "ù"},   {"uacute;", "ú"},
            {"ucirc;", "û"},    {"uuml;", "ü"},     {"yacute;", "ý"},   {"thorn;", "þ"},
            {"yuml;", "ÿ"}
        };

        private readonly static Dictionary<string, string> _entityReverseMap = new Dictionary<string, string>
        {
            {"\"", "&quot;"},   {"'", "&apos;"},    {"&", "&amp;"},     {"<", "&lt;"},
            {">", "&gt;"},      {"", "&nbsp;"},     {"¡", "&iexcl;"},   {"¢", "&cent;"},
            {"£", "&pound;"},   {"¤", "&curren;"},  {"¥", "&yen;"},     {"¦", "&brvbar;"},
            {"§", "&sect;"},    {"¨", "&uml;"},     {"©", "&copy;"},    {"ª", "&ordf;"},
            {"«", "&laquo;"},   {"¬", "&not;"},     {"­", "&shy;"},      {"®", "&reg;"},
            {"¯", "&macr;"},    {"°", "&deg;"},     {"±", "&plusmn;"},  {"²", "&sup2;"},
            {"³", "&sup3;"},    {"´", "&acute;"},   {"µ", "&micro;"},   {"¶", "&para;"},
            {"·", "&middot;"},  {"¸", "&cedil;"},   {"¹", "&sup1;"},    {"º", "&ordm;"},
            {"»", "&raquo;"},   {"¼", "&frac14;"},  {"½", "&frac12;"},  {"¾", "&frac34;"},
            {"¿", "&iquest;"},  {"×", "&times;"},   {"÷", "&divide;"},  {"À", "&Agrave;"},
            {"Á", "&Aacute;"},  {"Â", "&Acirc;"},   {"Ã", "&Atilde;"},  {"Ä", "&Auml;"},
            {"Å", "&Aring;"},   {"Æ", "&AElig;"},   {"Ç", "&Ccedil;"},  {"È", "&Egrave;"},
            {"É", "&Eacute;"},  {"Ê", "&Ecirc;"},   {"Ë", "&Euml;"},    {"Ì", "&Igrave;"},
            {"Í", "&Iacute;"},  {"Î", "&Icirc;"},   {"Ï", "&Iuml;"},    {"Ð", "&ETH;"},
            {"Ñ", "&Ntilde;"},  {"Ò", "&Ograve;"},  {"Ó", "&Oacute;"},  {"Ô", "&Ocirc;"},
            {"Õ", "&Otilde;"},  {"Ö", "&Ouml;"},    {"Ø", "&Oslash;"},  {"Ù", "&Ugrave;"},
            {"Ú", "&Uacute;"},  {"Û", "&Ucirc;"},   {"Ü", "&Uuml;"},    {"Ý", "&Yacute;"},
            {"Þ", "&THORN;"},   {"ß", "&szlig;"},   {"à", "&agrave;"},  {"á", "&aacute;"},
            {"â", "&acirc;"},   {"ã", "&atilde;"},  {"ä", "&auml;"},    {"å", "&aring;"},
            {"æ", "&aelig;"},   {"ç", "&ccedil;"},  {"è", "&egrave;"},  {"é", "&eacute;"},
            {"ê", "&ecirc;"},   {"ë", "&euml;"},    {"ì", "&igrave;"},  {"í", "&iacute;"},
            {"î", "&icirc;"},   {"ï", "&iuml;"},    {"ð", "&eth;"},     {"ñ", "&ntilde;"},
            {"ò", "&ograve;"},  {"ó", "&oacute;"},  {"ô", "&ocirc;"},   {"õ", "&otilde;"},
            {"ö", "&ouml;"},    {"ø", "&oslash;"},  {"ù", "&ugrave;"},  {"ú", "&uacute;"},
            {"û", "&ucirc;"},   {"ü", "&uuml;"},    {"ý", "&yacute;"},  {"þ", "&thorn;"},
            {"ÿ", "&yuml;"}
        };

        private readonly static Dictionary<string, string> _hexMap = new Dictionary<string, string>
        {
            {"9;","\t"},    {"xa;","\n"},   {"xd;","\r"},   {"20;"," "},
            {"21;","!"},    {"22;","\""},   {"23;","#"},    {"24;","$"},
            {"25;","%"},    {"26;","&"},    {"27;","'"},    {"28;","("},
            {"29;",")"},    {"2a;","*"},    {"2b;","+"},    {"2c;",","},

            {"2d;","-"},    {"2e;","."},    {"2f;","/"},    {"3a;",":"},
            {"3b;",";"},    {"3c;","<"},    {"3d;","="},    {"3e;",">"},
            {"3f;","?"},    {"40;","@"},    {"5b;","["},    {"5c;","\\"},
            {"5d;","]"},    {"5e;","^"},    {"60;","`"},    {"7b;","{"},

            {"a0;"," "},    {"a1;","¡"},    {"a2;","¢"},    {"a3;","£"},
            { "a4;","¤"},   {"a5;","¥"},    {"a6;","¦"},    {"a7;","§"},
            {"a9;","©"},    {"ab;","«"},    {"ae;","®"},    {"b0;","°"},
            {"b1;","±"},    {"b4;","´"},    {"b5;","µ"},    {"b6;","¶"},

            {"b7;","·"},    {"bb;","»"},    {"bc;","¼"},    {"bd;","½"},
            {"be;","¾"},    {"bf;","¿"},    {"f7;","÷"},    {"f8;","ø"}
        };

        private readonly static Dictionary<string, string> _numMap = new Dictionary<string, string>
        {
            {"9;","\t"},    {"10;","\n"},   {"13;","\r"},   {"32;"," "},
            {"33;","!"},    {"34;","\""},   {"35;","#"},    {"36;","$"},
            {"37;","%"},    {"38;","&"},    {"39;","'"},    {"40;","("},
            {"41;",")"},    {"42;","*"},    {"43;","+"},    {"44;",","},

            {"45;","-"},    {"46;","."},    {"47;","/"},    {"58;",":"},
            {"59;",";"},    {"60;","<"},    {"61;","="},    {"62;",">"},
            {"63;","?"},    {"64;","@"},    {"91;","["},    {"92;","\\"},
            {"93;","]"},    {"94;","^"},    {"96;","`"},    {"123;","{"},

            {"160;"," "},   {"161;","¡"},   {"162;","¢"},   {"163;","£"},
            {"164;","¤"},   {"165;","¥"},   {"166;","¦"},   {"167;","§"},
            {"169;","©"},   {"171;","«"},   {"174;","®"},   {"176;","°"},
            {"177;","±"},   {"180;","´"},   {"181;","µ"},   {"182;","¶"},

            {"183;","·"},   {"187;","»"},   {"188;","¼"},   {"189;","½"},
            {"190;","¾"},   {"191;","¿"},   {"247;","÷"},   {"248;","ø"}
        };

        #endregion Mapping

        public static string DecodeHtmlEntities(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return line;
            var result = new StringBuilder();
            var cursor = new StringCursor(line);
            bool found = false;
            do
            {
                found = false;
                if (cursor.EOF) break;
                switch (cursor.Current)
                {
                    case '&':
                        var buf = cursor.Token.Next.Next;
                        switch (cursor.Next)
                        {
                            case '#':   //  HEX or DEC
                                switch (buf.Value)
                                {
                                    case 'x':   //  HEX
                                        buf = buf.Next;
                                        foreach (var hexPair in _hexMap)
                                        {
                                            if (buf.Test(hexPair.Key))
                                            {
                                                cursor.MoveNext();
                                                cursor.MoveNext();
                                                cursor.MoveNext();
                                                cursor.MoveAfter(hexPair.Key);
                                                result.Append(hexPair.Value);
                                                found = true;
                                                break;
                                            }
                                        }
                                        break;

                                    default:    //  DEC
                                        foreach (var decPair in _numMap)
                                        {
                                            if (buf.Test(decPair.Key))
                                            {
                                                cursor.MoveNext();
                                                cursor.MoveNext();
                                                cursor.MoveAfter(decPair.Key);
                                                result.Append(decPair.Value);
                                                found = true;
                                                break;
                                            }
                                        }
                                        break;
                                }
                                break;

                            default:    //  Entity
                                foreach (var pair in _entityMap)
                                {
                                    if (cursor.Token.Next.Test(pair.Key))
                                    {
                                        cursor.MoveNext();
                                        cursor.MoveAfter(pair.Key);
                                        result.Append(pair.Value);
                                        found = true;
                                        break;
                                    }
                                }
                                break;
                        }
                        if (false == found)
                        {
                            result.Append(cursor.Current);
                        }
                        break;

                    default:
                        result.Append(cursor.Current);
                        break;
                }
            } while (found || cursor.MoveNext());
            return result.ToString();
        }

        public static string EncodeHtmlEntities(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return line;
            var result = new StringBuilder();
            var cursor = new TStringReader(line);
            while (cursor.EOF == false)
            {
                if (cursor.Current == '&')
                {
                    cursor.Move();
                    var identity = cursor.ReadIdentity().ToLowerInvariant();
                    cursor.MoveBack();
                    if (_entityMap.ContainsKey(identity + ";"))
                    {
                        result.Append(cursor.Current);
                    }
                    else
                    {
                        result.Append(_entityReverseMap["&"]);
                    }
                }
                else if (_entityReverseMap.ContainsKey(cursor.Current.ToString()))
                {
                    result.Append(_entityReverseMap[cursor.Current.ToString()]);
                }
                else
                {
                    result.Append(cursor.Current);
                }
                cursor.Move();
            }
            return result.ToString();
        }
    }
}