using System;
using System.Collections.Generic;
using ZeroLevel.Services.Collections;

/*
     Example with text lines
        var freg_dict = BuildNGramm(File.ReadAllLines("samples.txt"), line => _provider.ExtractLexTokens(line).Where(w => StopWords.IsStopWord(w.Token) == false).Select(t => t.Token.ToLowerInvariant()), 2);

     Example with sentences
        var freg_dict = BuildNGramm<Sentence>(TAStringReader.ReadSentenses(File.ReadAllText("samples.txt")), sent => sent.Tokens.Select(t => t.Value).Where(w => StopWords.IsStopWord(w) == false), 3);
*/

namespace ZeroLevel.Services.Semantic.Helpers
{
    public static class NGramms
    {
        public static Dictionary<string, int> BuildNGramm(IEnumerable<string> input, Func<string, IEnumerable<string>> tokenizer, int N)
        {
            var ngramms = new Dictionary<string, int>();
            var arr = new FixSizeQueue<string>(N);
            foreach (var line in input)
            {
                foreach (var token in tokenizer(line))
                {
                    arr.Push(token);
                    if (arr.Count == N)
                    {
                        var currentPrase = string.Join(" ", arr.Dump());
                        if (ngramms.ContainsKey(currentPrase)) ngramms[currentPrase]++;
                        else ngramms.Add(currentPrase, 1);
                    }
                }
                while (arr.Count > 0)
                    arr.Take();
            }
            return ngramms;
        }        

        public static Dictionary<string, int> GetUnigramms(IEnumerable<string> input, Func<string, IEnumerable<string>> tokenizer)
            => BuildNGramm(input, tokenizer, 1);

        public static Dictionary<string, int> GetBigramms(IEnumerable<string> input, Func<string, IEnumerable<string>> tokenizer)
            => BuildNGramm(input, tokenizer, 2);

        public static Dictionary<string, int> GetTrigramms(IEnumerable<string> input, Func<string, IEnumerable<string>> tokenizer)
            => BuildNGramm(input, tokenizer, 3);


        public static Dictionary<string, int> BuildNGramm<T>(IEnumerable<T> input, Func<T, IEnumerable<string>> tokenizer, int N)
        {
            var ngramms = new Dictionary<string, int>();
            var arr = new FixSizeQueue<string>(N);
            foreach (var item in input)
            {
                foreach (var token in tokenizer(item))
                {
                    arr.Push(token);
                    if (arr.Count == N)
                    {
                        var currentPrase = string.Join(" ", arr.Dump());
                        if (ngramms.ContainsKey(currentPrase)) ngramms[currentPrase]++;
                        else ngramms.Add(currentPrase, 1);
                    }
                }
                while (arr.Count > 0)
                    arr.Take();
            }
            return ngramms;
        }

        public static Dictionary<string, int> GetUnigramms<T>(IEnumerable<T> input, Func<T, IEnumerable<string>> tokenizer)
            => BuildNGramm(input, tokenizer, 1);

        public static Dictionary<string, int> GetBigramms<T>(IEnumerable<T> input, Func<T, IEnumerable<string>> tokenizer)
            => BuildNGramm(input, tokenizer, 2);

        public static Dictionary<string, int> GetTrigramms<T>(IEnumerable<T> input, Func<T, IEnumerable<string>> tokenizer)
            => BuildNGramm(input, tokenizer, 3);

    }
}
