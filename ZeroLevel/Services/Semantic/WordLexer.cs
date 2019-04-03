namespace ZeroLevel.Services.Semantic
{
    public class WordLexer : ILexer
    {
        public string Lex(string word)
        {
            return word.Trim().ToLowerInvariant();
        }
    }
}