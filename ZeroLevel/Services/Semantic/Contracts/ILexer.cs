namespace ZeroLevel.Services.Semantic
{
    /// <summary>
    /// Выполняет преобразование слова к абстрактной основе слова(корень, стем, лемма и т.п.)
    /// </summary>
    public interface ILexer
    {
        string Lex(string word);
    }
}
