using Iveonik.Stemmers;

namespace ZeroLevel.Services.Semantic
{
    public static class LexProviderFactory
    {
        public static ILexProvider CreateProvider(ILexer lexer)
        {
            return new LexProvider(lexer);
        }

        public static ILexProvider CreateSimpleTextProvider()
        {
            return CreateProvider(new WordLexer());
        }

        public static ILexProvider CreateStemProvider(Languages language)
        {
            ILexer lexer = null;
            switch (language)
            {
                case Languages.Czech:
                    lexer = new CzechStemmer();
                    break;

                case Languages.Danish:
                    lexer = new DanishStemmer();
                    break;

                case Languages.Dutch:
                    lexer = new DutchStemmer();
                    break;

                case Languages.English:
                    lexer = new EnglishStemmer();
                    break;

                case Languages.Finnish:
                    lexer = new FinnishStemmer();
                    break;

                case Languages.French:
                    lexer = new FrenchStemmer();
                    break;

                case Languages.German:
                    lexer = new GermanStemmer();
                    break;

                case Languages.Hungarian:
                    lexer = new HungarianStemmer();
                    break;

                case Languages.Italian:
                    lexer = new ItalianStemmer();
                    break;

                case Languages.Norwegian:
                    lexer = new NorwegianStemmer();
                    break;

                case Languages.Portugal:
                    lexer = new PortugalStemmer();
                    break;

                case Languages.Romanian:
                    lexer = new RomanianStemmer();
                    break;

                case Languages.Spanish:
                    lexer = new SpanishStemmer();
                    break;

                case Languages.Russian:
                    lexer = new RussianStemmer();
                    break;

                default:
                    lexer = new RussianStemmer();
                    break;
            }
            return new LexProvider(lexer);
        }
    }
}