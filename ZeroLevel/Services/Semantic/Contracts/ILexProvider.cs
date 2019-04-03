using System.Collections.Generic;

namespace ZeroLevel.Services.Semantic
{
    public interface ILexProvider
    {
        /// <summary>
        /// Extract tokens from text as is
        /// </summary>
        /// <returns>Spisok tokenov</returns>
        IEnumerable<LexToken> ExtractLexTokens(string text);

        /// <summary>
        /// Selecting unique tokens from text
        /// </summary>
        /// <returns>Tokens</returns>
        IEnumerable<LexToken> ExtractUniqueLexTokens(string text);

        /// <summary>
        /// Allocation of unique tokens from text with drop of stop words
        /// </summary>
        /// <returns>Tokens</returns>
        IEnumerable<LexToken> ExtractUniqueLexTokensWithoutStopWords(string text);

        /// <summary>
        /// Search for tokens in the text corresponding to the specified words (full-text search)
        /// </summary>
        /// <param name="text">Search text</param>
        /// <param name="words">Search words</param>
        /// <returns>Dictionary, where key is a word, value is a list of matching tokens found for it</returns>
        IDictionary<string, IEnumerable<LexToken>> SearchLexTokensByWords(string text, string[] words);

        /// <summary>
        /// Search for tokens in the text corresponding to the specified phrases (full-text search)
        /// </summary>
        /// <param name="text">Search text</param>
        /// <param name="phrases">Search phrases</param>
        /// <returns>The dictionary, where the key is a phrase, a value is a list of token arrays corresponding to it</returns>
        IDictionary<string, IEnumerable<LexToken[]>> SearchLexTokensByPhrases(string text, string[] phrases);
    }
}