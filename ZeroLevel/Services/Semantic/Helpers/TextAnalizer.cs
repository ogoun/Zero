using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZeroLevel.Services.Semantic;

namespace ZeroLevel.Implementation.Semantic.Helpers
{
    public static class TextAnalizer
    {
        internal static readonly Regex ReWord = new Regex("\\b[\\wА-Яа-я-’]+\\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Выделение слов из текста
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список слов</returns>
        public static IEnumerable<string> ExtractWords(string text)
        {
            var result = new List<string>();
            foreach (Match match in ReWord.Matches(text))
            {
                result.Add(match.Value);
            }

            return result;
        }

        /// <summary>
        /// Выделение уникальных слов из текста
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список уникальных слов</returns>
        public static IEnumerable<string> ExtractUniqueWords(string text)
        {
            return new HashSet<string>(ExtractWords(text));
        }

        /// <summary>
        /// Выделение уникальных слов из текста без стоп слов
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список уникальных слов без стоп слов</returns>
        public static IEnumerable<string> ExtractUniqueWordsWithoutStopWords(string text)
        {
            return new HashSet<string>(ExtractUniqueWords(text).Where(w => StopWords.IsStopWord(w) == false));
        }

        /// <summary>
        /// Выделение токенов из текста
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список токенов</returns>
        public static IEnumerable<WordToken> ExtractWordTokens(string text)
        {
            var result = new List<WordToken>();
            foreach (Match match in ReWord.Matches(text))
            {
                result.Add(new WordToken(match.Value, match.Index));
            }

            return result;
        }

        /// <summary>
        /// Выделение уникальных токенов из текста (первое вхождение)
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список уникальных токенов</returns>
        public static IEnumerable<WordToken> ExtractUniqueWordTokens(string text)
        {
            return ExtractWordTokens(text).DistinctBy(t => t.Word);
        }

        /// <summary>
        /// Выделение уникальных токенов из текста с отбрасыванием стоп-слов
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список уникальных токенов без стоп слов</returns>
        public static IEnumerable<WordToken> ExtractUniqueWordTokensWithoutStopWords(string text)
        {
            return ExtractWordTokens(text).DistinctBy(t => t.Word).Where(t => StopWords.IsStopWord(t.Word) == false);
        }
    }
}