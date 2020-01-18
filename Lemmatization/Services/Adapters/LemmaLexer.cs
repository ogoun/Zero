using LemmaSharp;
using ZeroLevel.Services.Semantic;

namespace Lemmatization
{
    public class LemmaLexer 
        : ILexer
    {
        private readonly ILemmatizer _lemmatizer;

        public LemmaLexer()
        {
            _lemmatizer = new LemmatizerPrebuiltFull(LanguagePrebuilt.Russian);
        }

        public string Lex(string word) 
        { 
            return _lemmatizer.Lemmatize(word.Trim().ToLowerInvariant()); 
        }
    }
}
