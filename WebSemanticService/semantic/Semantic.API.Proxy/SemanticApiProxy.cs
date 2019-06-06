using System.Collections.Generic;
using ZeroLevel.Services.Semantic;

namespace Semantic.API.Proxy
{
    /// <summary>
    /// Предоставляет доступ к Prime Semantic API 
    /// </summary>
    public sealed class SemanticApiProxy 
        : BaseProxy
    {
        public SemanticApiProxy(string baseUri)
            : base(baseUri)
        {
        }

        #region Split to words
        /// <summary>
        /// Разделение текста на слова
        /// </summary>
        /// <returns>Список слов</returns>
        public IEnumerable<LexToken> ExtractWords(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/text/words", text);
        }
        /// <summary>
        /// Разделение текста на слова, без повторов
        /// </summary>
        /// <returns>Список слов</returns>
        public IEnumerable<LexToken> ExtractUniqueWords(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/text/words/unique", text);
        }
        /// <summary>
        /// Разделение текста на слова без стоп-слов и повторов
        /// </summary>
        /// <returns>Список слов</returns>
        public IEnumerable<LexToken> ExtractUniqueWordsWithoutStopWords(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/text/words/clean", text);
        }
        #endregion

        #region Stemming
        /// <summary>
        /// Разделение текста на стемы (основы слов)
        /// </summary>
        /// <returns>Список стемов</returns>
        public IEnumerable<LexToken> ExtractStems(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/stem", text);
        }
        /// <summary>
        /// Разделение текста на стемы (основы слов) без повторов
        /// </summary>
        /// <returns>Список стемов</returns>
        public IEnumerable<LexToken> ExtractUniqueStems(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/stem/unique", text);
        }
        /// <summary>
        /// Разделение текста на токены, на основе стемов
        /// </summary>
        /// <returns>Список токенов (оригинальное слово, стем, позиция в тексте)</returns>
        public IEnumerable<LexToken> ExtractUniqueStemsWithoutStopWords(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/stem/clean", text);
        }
        #endregion

        #region Lemmatization
        /// <summary>
        /// Разделение текста на леммы (начальные формы слов)
        /// </summary>
        /// <returns>Список лемм</returns>
        public IEnumerable<LexToken> ExtractLemmas(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/lemma", text);
        }
        /// <summary>
        /// Разделение текста на леммы (начальные формы слов) без повторов
        /// </summary>
        /// <returns>Список лемм</returns>
        public IEnumerable<LexToken> ExtractUniqueLemmas(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/lemma/unique", text);
        }
        /// <summary>
        /// Разделение текста на леммы (начальные формы слов) без повторов и стоп-слов
        /// </summary>
        /// <returns>Список лемм</returns>
        public IEnumerable<LexToken> ExtractUniqueLemmasWithoutStopWords(string text)
        {
            return Post<IEnumerable<LexToken>>("/api/lemma/clean", text);
        }
        #endregion

        #region Words occurences
        /// <summary>
        /// Поиск вхождений слов в текст
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="words">Массив слов для поиска</param>
        /// <returns>Список токенов (слово, позиция)</returns>
        public IDictionary<string, IEnumerable<LexToken>> SearchWordsInTextDirectly(string text, string[] words)
        {
            return Post<IDictionary<string, IEnumerable<LexToken>>>("/api/text/occurences/words", new WordsSearchRequest
            {
                Text = text,
                Words = words
            });
        }
        /// <summary>
        /// Поиск вхождений слов в текст, на основе стемминга
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="words">Массив слов для поиска</param>
        /// <returns>Список токенов (слово, стем, позиция)</returns>
        public IDictionary<string, IEnumerable<LexToken>> SearchWordsInTextByStemming(string text, string[] words)
        {
            return Post<IDictionary<string, IEnumerable<LexToken>>>("/api/stem/occurences/words", new WordsSearchRequest
            {
                Text = text,
                Words = words
            });
        }
        /// <summary>
        /// Поиск вхождений слов в текст, на основе лемматизации
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="words">Массив слов для поиска</param>
        /// <returns>Список токенов (слово, лемма, позиция)</returns>
        public IDictionary<string, IEnumerable<LexToken>> SearchWordsInTextByLemmas(string text, string[] words)
        {
            return Post<IDictionary<string, IEnumerable<LexToken>>>("/api/lemma/occurences/words", new WordsSearchRequest
            {
                Text = text,
                Words = words
            });
        }
        #endregion

        #region Phrase occurences
        /// <summary>
        /// Поиск вхождений фраз в текст
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="phrases">Массив фраз для поиска</param>
        /// <returns>Список фраз в тексте соответствующих поисковому запросу</returns>
        public IDictionary<string, IEnumerable<LexToken[]>> SearchPhrasesInTextDirectly(string text, string[] phrases)
        {
            return Post<IDictionary<string, IEnumerable<LexToken[]>>>("/api/text/occurences/phrases", new WordsSearchRequest
            {
                Text = text,
                Words = phrases
            });
        }
        /// <summary>
        /// Поиск вхождений фраз в текст, на основе стемминга
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="phrases">Массив фраз для поиска</param>
        /// <returns>Список фраз в тексте соответствующих поисковому запросу</returns>
        public IDictionary<string, IEnumerable<LexToken[]>> SearchPhrasesInTextByStemming(string text, string[] phrases)
        {
            return Post<IDictionary<string, IEnumerable<LexToken[]>>>("/api/stem/occurences/phrases", new WordsSearchRequest
            {
                Text = text,
                Words = phrases
            });
        }
        /// <summary>
        /// Поиск вхождений фраз в текст, на основе лемматизации
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="phrases">Массив фраз для поиска</param>
        /// <returns>Список фраз в тексте соответствующих поисковому запросу</returns>
        public IDictionary<string, IEnumerable<LexToken[]>> SearchPhrasesInTextByLemmas(string text, string[] phrases)
        {
            return Post<IDictionary<string, IEnumerable<LexToken[]>>>("api/lemma/occurences/phrases", new WordsSearchRequest
            {
                Text = text,
                Words = phrases
            });
        }
        #endregion
    }
}
