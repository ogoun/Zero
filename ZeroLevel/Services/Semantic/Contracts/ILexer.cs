namespace ZeroLevel.Services.Semantic
{
    /// <summary>
    /// Performs word conversion to abstract word basis (root, stem, lemma, etc.)
    /// </summary>
    public interface ILexer
    {
        string Lex(string word);
    }
}