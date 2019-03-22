using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZeroLevel.Implementation.Semantic.Helpers;

namespace ZeroLevel.Services.Semantic
{
    public class LexProvider : ILexProvider
    {
        private readonly ILexer _lexer;
        private static LexToken _empty = new LexToken(string.Empty, string.Empty, -1);

        public LexProvider(ILexer lexer)
        {
            if (null == lexer)
                throw new ArgumentNullException(nameof(lexer));
            _lexer = lexer;
        }

        public IEnumerable<LexToken> ExtractLexTokens(string text)
        {
            var result = new List<LexToken>();
            foreach (Match match in TextAnalizer.ReWord.Matches(text))
            {
                result.Add(new LexToken(match.Value, _lexer.Lex(match.Value), match.Index));
            }

            return result;
        }

        public IEnumerable<LexToken> ExtractUniqueLexTokens(string text)
        {
            return TextAnalizer.ExtractUniqueWordTokens(text)
                .Select(w => new LexToken(w.Word, _lexer.Lex(w.Word), w.Position)).DistinctBy(s => s.Token);
        }

        public IEnumerable<LexToken> ExtractUniqueLexTokensWithoutStopWords(string text)
        {
            return TextAnalizer.ExtractUniqueWordTokensWithoutStopWords(text)
                .Select(w => new LexToken(w.Word, _lexer.Lex(w.Word), w.Position)).DistinctBy(s => s.Token);
        }

        public IDictionary<string, IEnumerable<LexToken>> SearchLexTokensByWords(string text, string[] words)
        {
            var result = new Dictionary<string, IEnumerable<LexToken>>();
            if (false == string.IsNullOrWhiteSpace(text) && words != null)
            {
                var textWords = ExtractLexTokens(text).ToList();
                var keywords = words.Select(w => new Tuple<string, string>(w, _lexer.Lex(w)));
                foreach (var key in keywords)
                {
                    var keyOcurrences = textWords.Where(l => l.Token.Equals(key.Item2, StringComparison.Ordinal));
                    if (keyOcurrences != null && keyOcurrences.Any())
                    {
                        result.Add(key.Item1, keyOcurrences);
                    }
                }
            }

            return result;
        }

        public IDictionary<string, IEnumerable<LexToken[]>> SearchLexTokensByPhrases(string text, string[] phrases)
        {
            var result = new Dictionary<string, IEnumerable<LexToken[]>>();
            if (false == string.IsNullOrWhiteSpace(text) && phrases != null)
            {
                var text_tokens = ExtractLexTokens(text).ToList();
                foreach (var phrase in phrases)
                {
                    var occurences = GetPhraseOccurrenceInText(text_tokens, phrase);
                    if (occurences.Count > 0)
                    {
                        result.Add(phrase, occurences);
                    }
                }
            }

            return result;
        }

        #region Helpers

        private List<LexToken[]> GetPhraseOccurrenceInText(List<LexToken> tokens, string phrase)
        {
            var result = new List<LexToken[]>();
            if (false == string.IsNullOrWhiteSpace(phrase))
            {
                var phrase_stems = ExtractLexTokens(phrase).ToArray();
                if (phrase_stems.Length > 0)
                {
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        if (tokens[i].Token.Equals(phrase_stems[0].Token, StringComparison.Ordinal))
                        {
                            var buffer = new LexToken[phrase_stems.Length];
                            buffer[0] = tokens[i];
                            int k = 1;
                            for (; k < phrase_stems.Length; k++)
                            {
                                if ((k + i) >= tokens.Count ||
                                    tokens[k + i].Token.Equals(phrase_stems[k].Token, StringComparison.Ordinal) ==
                                    false)
                                    break;
                                buffer[k] = tokens[k + i];
                            }

                            if (k == phrase_stems.Length)
                            {
                                result.Add(buffer);
                            }
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}