using System.Collections.Generic;

namespace ZeroLevel.Services.Semantic
{
    public interface ILexProvider
    {
        /// <summary>
        /// Выделение токенов из текста как есть
        /// </summary>
        /// <returns>Список токенов</returns>
        IEnumerable<LexToken> ExtractLexTokens(string text);
        /// <summary>
        /// Выделение уникальных токенов из текста
        /// </summary>
        /// <returns>Список токенов</returns>
        IEnumerable<LexToken> ExtractUniqueLexTokens(string text);
        /// <summary>
        /// Выделение уникальных токенов из текста с отбрасыванием стоп-слов
        /// </summary>
        /// <returns>Список токенов</returns>
        IEnumerable<LexToken> ExtractUniqueLexTokensWithoutStopWords(string text);
        /// <summary>
        /// Поиск токенов в тексте соответствующих указанным словам (полнотекстовый поиск)
        /// </summary>
        /// <param name="text">Текст по которому выполняется поиск</param>
        /// <param name="words">Слова для поиска</param>
        /// <returns>Словарь, где ключ - слово, значение - список соответствующих ему найденных токенов</returns>
        IDictionary<string, IEnumerable<LexToken>> SearchLexTokensByWords(string text, string[] words);
        /// <summary>
        /// Поиск токенов в тексте соответствующих указанным фразам (полнотекстовый поиск)
        /// </summary>
        /// <param name="text">Текст по которому выполняется поиск</param>
        /// <param name="phrases">Фразы для поиска</param>
        /// <returns>Словарь, где ключ - фраза, значение - список соответствующих ему найденных массивов токенов</returns>
        IDictionary<string, IEnumerable<LexToken[]>> SearchLexTokensByPhrases(string text, string[] phrases);
    }
}
