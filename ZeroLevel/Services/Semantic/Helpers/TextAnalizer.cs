﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZeroLevel.Services.Semantic;

namespace ZeroLevel.Implementation.Semantic.Helpers
{
    public static class TextAnalizer
    {
        internal static readonly Regex ReWord = new Regex("\\b[\\wА-Яа-я-’]+\\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static readonly Regex ReRuWord = new Regex("\\b[А-Яа-я-]+\\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Highlighting words from text
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Words</returns>
        public static IEnumerable<string> ExtractWords(string text)
        {
            foreach (Match match in ReWord.Matches(text))
            {
                yield return match.Value;
            }
        }

        public static IEnumerable<string> ExtractRuWords(string text)
        {
            foreach (Match match in ReRuWord.Matches(text))
            {
                yield return match.Value;
            }
        }

        /// <summary>
        /// Highlighting unique words from text
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>List of unique words</returns>
        public static IEnumerable<string> ExtractUniqueWords(string text)
        {
            return new HashSet<string>(ExtractWords(text));
        }

        /// <summary>
        /// Highlighting unique words from text without stop words
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>List of unique words without stop words</returns>
        public static IEnumerable<string> ExtractUniqueWordsWithoutStopWords(string text)
        {
            return new HashSet<string>(ExtractUniqueWords(text).Where(w => StopWords.IsStopWord(w) == false));
        }

        /// <summary>
        /// Extract tokens from text
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Tokens</returns>
        public static IEnumerable<WordToken> ExtractWordTokens(string text)
        {
            foreach (Match match in ReWord.Matches(text))
            {
                yield return new WordToken(match.Value, match.Index);
            }
        }

        public static IEnumerable<WordToken> ExtractWordTokens(string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                yield return new WordToken(words[i], i);
            }
        }

        /// <summary>
        /// Selection of unique tokens from the text (first entry)
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>List of unique tokens</returns>
        public static IEnumerable<WordToken> ExtractUniqueWordTokens(string text)
        {
            return ExtractWordTokens(text).DistinctBy(t => t.Word);
        }

        public static IEnumerable<WordToken> ExtractUniqueWordTokens(string[] words)
        {
            return ExtractWordTokens(words).DistinctBy(t => t.Word);
        }

        /// <summary>
        /// Allocation of unique tokens from text with drop of stop words
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>List of unique tokens without stop words</returns>
        public static IEnumerable<WordToken> ExtractUniqueWordTokensWithoutStopWords(string text)
        {
            return ExtractWordTokens(text).DistinctBy(t => t.Word).Where(t => StopWords.IsStopWord(t.Word) == false);
        }

        public static IEnumerable<WordToken> ExtractUniqueWordTokensWithoutStopWords(string[] words)
        {
            return ExtractWordTokens(words).DistinctBy(t => t.Word).Where(t => StopWords.IsStopWord(t.Word) == false);
        }
    }
}