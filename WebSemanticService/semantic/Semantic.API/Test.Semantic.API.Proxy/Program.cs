using Semantic.API.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Semantic;

namespace Test.Semantic.API.Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = new SemanticApiProxy("http://localhost:8020");
            var text = "Мы вполне привыкли к трём пространственным измерениям нашей Вселенной, к длине, ширине и глубине. Мы можем представить, как выглядят вещи в меньших измерениях – на двумерной плоскости или на одномерной линии – но с высшими измерениями всё не так просто, поскольку мы не можем представить себе движение в направлении, не описываемом нашим привычным пространством. Во Вселенной есть и четвёртое измерение (время), и только три пространственных. Но среди вопросов на этой неделе я увидел выдающийся вопрос из серии «что, если» от писателя Келли Люк: Что для людей означало бы, если бы количество измерений в нашем мире менялось бы как времена года? Например, половина года у нас три измерения, а половина – четыре. Представьте, по возможности, что у вас есть способность двигаться в одном дополнительном направлении – не входящем в обычный набор вверх-вниз, север - юг и запад-восток.Представьте, что такая способность есть только у вас";
            var direct_words = new string[] { "представить", "пространством", "измерения" };
            var words = new string[] { "представлять", "пространство", "измерение", "писатель" };

            var direct_phrases = new string[] { "Мы вполне привыкли", "описываемом нашим привычным", "среди вопросов" };
            var phrases = new string[] { "высшие измерения", "наш мир", "двумерная плоскость", "все не так просто" };
            // 1. Split
            TestSplitTextIntoWords(proxy, text);
            Console.ReadKey();
            Console.Clear();
            // 2. Stemming
            TestSplitTextIntoStems(proxy, text);
            Console.ReadKey();
            Console.Clear();
            // 3. Lemmatization
            TestSplitTextIntoLemmas(proxy, text);
            Console.ReadKey();
            Console.Clear();
            // 4. Search words
            TestSearchWordsInText(proxy, text, direct_words, words);
            Console.ReadKey();
            Console.Clear();
            // 5. Search phrases
            TestSearchPhrasesInText(proxy, text, direct_phrases, phrases);
            Console.ReadKey();
            Console.Clear();

        }

        private static void TestSplitTextIntoWords(SemanticApiProxy proxy, string text)
        {
            Console.WriteLine("Разбиение на слова");
            Console.WriteLine(text);
            Console.WriteLine("Words:");
            ShowLines(proxy.ExtractWords(text));
            Console.WriteLine("Unique words:");
            ShowLines(proxy.ExtractUniqueWords(text));
            Console.WriteLine("Words dictionary:");
            ShowLines(proxy.ExtractUniqueWordsWithoutStopWords(text));
            Console.WriteLine("Completed. Press key to continue...");
        }

        private static void TestSplitTextIntoStems(SemanticApiProxy proxy, string text)
        {
            Console.WriteLine("Разбиение на стемы");
            Console.WriteLine(text);
            Console.WriteLine("Stems:");
            ShowLines(proxy.ExtractStems(text));
            Console.WriteLine("Stem tokens:");
            ShowLines(proxy.ExtractUniqueStems(text));
            Console.WriteLine("Stems dictionary:");
            ShowLines(proxy.ExtractUniqueStemsWithoutStopWords(text));
            Console.WriteLine("Completed. Press key to continue...");
        }

        private static void TestSplitTextIntoLemmas(SemanticApiProxy proxy, string text)
        {
            Console.WriteLine("Разбиение на леммы");
            Console.WriteLine(text);
            Console.WriteLine("Lemmas:");
            ShowLines(proxy.ExtractLemmas(text));
            Console.WriteLine("Unique lemmas:");
            ShowLines(proxy.ExtractUniqueLemmas(text));
            Console.WriteLine("Lemmas dictionary:");
            ShowLines(proxy.ExtractUniqueLemmasWithoutStopWords(text));
            Console.WriteLine("Completed. Press key to continue...");
        }

        private static void TestSearchWordsInText(SemanticApiProxy proxy, string text, string[] direct_words, string[] words)
        {
            Console.WriteLine("Поиск слов в текст");
            Console.WriteLine(text);
            Console.WriteLine(string.Join("; ", words));
            Console.WriteLine(string.Join("; ", direct_words));
            Console.WriteLine("GET");
            Console.WriteLine("Прямой поиск слов:");
            ShowLines(proxy.SearchWordsInTextDirectly(text, direct_words));
            Console.WriteLine("Поиск слов по стемам:");
            ShowLines(proxy.SearchWordsInTextByStemming(text, words));
            Console.WriteLine("Поиск слов по леммам:");
            ShowLines(proxy.SearchWordsInTextByLemmas(text, words));
            Console.WriteLine("Completed. Press key to continue...");
        }

        private static void TestSearchPhrasesInText(SemanticApiProxy proxy, string text, string[] direct_phrases, string[] phrases)
        {
            Console.WriteLine("Поиск фраз в тексте");
            Console.WriteLine(text);
            Console.WriteLine("Прямой поиск фраз:");
            ShowLines(proxy.SearchPhrasesInTextDirectly(text, direct_phrases));
            Console.WriteLine("Поиск фраз по стемам:");
            ShowLines(proxy.SearchPhrasesInTextByStemming(text, phrases));
            Console.WriteLine("Поиск фраз по леммам:");
            ShowLines(proxy.SearchPhrasesInTextByLemmas(text, phrases));
            Console.WriteLine("Completed. Press key to continue...");
        }

        private static void ShowLines(IDictionary<string, IEnumerable<LexToken[]>> lexems)
        {
            foreach (var pair in lexems)
            {
                Console.Write(pair.Key);
                foreach (var l in pair.Value)
                {
                    Console.Write($"\t[{string.Join("; ", l.Select(e => e.Token))}]");
                }
            }
            Console.WriteLine();
        }

        private static void ShowLines(IEnumerable<LexToken> lines)
        {
            var columns_count = 4;
            var list = lines.ToList();
            var dif = list.Count - list.Count % columns_count;
            for (int i = 0; i < dif; i += columns_count)
            {
                for (var j = 0; j < columns_count; j++)
                {
                    Console.Write("\t{0}[{1}]", list[i + j].Token, list[i + j].Word);
                }
                Console.WriteLine();
            }
            for (var j = list.Count - dif; j > 0; j--)
            {
                Console.Write("\t{0}[{1}]", list[list.Count - j].Token, list[list.Count - j].Word);
            }
            Console.WriteLine();
        }

        private static void ShowLines(IDictionary<string, IEnumerable<LexToken>> lexems)
        {
            foreach (var pair in lexems)
            {
                Console.Write(pair.Key);
                foreach (var l in pair.Value)
                {
                    Console.Write($"\t{l.Token} ({l.Position})");
                }
            }
            Console.WriteLine();
        }
    }
}
