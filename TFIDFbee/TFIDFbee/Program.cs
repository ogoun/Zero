using Lemmatization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TFIDFbee.Reader;
using ZeroLevel;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Semantic.Helpers;
using ZeroLevel.Services.Serialization;

namespace TFIDFbee
{
    class Program
    {
        private const string source = @"E:\Desktop\lenta-ru-data-set_19990901_20171204\lenta-ru-data-set_19990901_20171204.json";
        private readonly static ILexProvider _lexer = new LexProvider(new LemmaLexer());

        static void Main(string[] args)
        {
            Log.AddConsoleLogger(ZeroLevel.Logging.LogLevel.FullDebug);
            Configuration.Save(Configuration.ReadFromApplicationConfig());
            IDocumentReader reader = new StateMachineReader(source, s => ExtractLemmas(s));

            BagOfWords codebook;
            if (File.Exists("model.bin"))
            {
                Log.Info("Load model from file");
                using (var stream = new FileStream("model.bin", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    codebook = MessageSerializer.Deserialize<BagOfWords>(stream);
                }
            }
            else
            {
                Log.Info("Create and train model");
                codebook = new BagOfWords();
                foreach (var batch in reader.ReadBatches(1000))
                {
                    codebook.Learn(batch);
                    Log.Info($"\r\n\tDocuments: {codebook.NumberOfDocuments}\r\n\tWords: {codebook.NumberOfWords}");
                }
                using (var stream = new FileStream("model.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    MessageSerializer.Serialize<BagOfWords>(stream, codebook);
                }
            }

            Log.Info("Build document vectors");
            List<SparceVector> vectors;
            if (File.Exists("vectors.bin"))
            {
                Log.Info("Load vectors from file");
                using (var stream = new FileStream("vectors.bin", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    vectors = MessageSerializer.DeserializeCompatible<List<SparceVector>>(stream);
                }
            }
            else
            {
                Log.Info("Create vectors");
                vectors = new List<SparceVector>();
                foreach (var docs in reader.ReadRawDocumentBatches(1000))
                {
                    foreach (var doc in docs)
                    {
                        var words = _lexer.ExtractLexTokens(doc.Item2).Select(t => t.Token).Concat(_lexer.ExtractLexTokens(doc.Item1).Select(t => t.Token)).ToArray();
                        vectors.Add(codebook.Transform(words));
                    }
                }
                using (var stream = new FileStream("vectors.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    MessageSerializer.SerializeCompatible<List<SparceVector>>(stream, vectors);
                }
            }

            Log.Info("Find similar documents");
            var list = new List<Tuple<double, int, int>>();
            long total_count = (vectors.Count * vectors.Count);
            long count = 0;
            for (int i = 0; i < vectors.Count; i++)
            {
                for (int j = i + 1; j < vectors.Count - 1; j++)
                {
                    count++;
                    if (count % 100000 == 0)
                    {
                        Log.Info($"Progress: {(int)(count * 100.0d / (double)total_count)} %.\tFound similars: {list.Count}.");
                    }
                    if (i == j) continue;
                    var diff = vectors[i].Measure(vectors[j]);
                    if (diff > 0.885d)
                    {
                        list.Add(Tuple.Create(diff, i, j));
                    }
                }
            }

            Log.Info("Prepare to show similar documents");
            var to_present = list.OrderBy(e => e.Item1).Take(2000).ToArray();
            var to_present_map = new Dictionary<int, Tuple<string, string>>();
            foreach (var e in to_present)
            {
                if (!to_present_map.ContainsKey(e.Item2))
                {
                    to_present_map.Add(e.Item2, null);
                }
                if (!to_present_map.ContainsKey(e.Item3))
                {
                    to_present_map.Add(e.Item3, null);
                }
            }

            int index = 0;
            foreach (var docs in reader.ReadRawDocumentBatches(1000))
            {
                foreach (var doc in docs)
                {
                    if (to_present_map.ContainsKey(index))
                    {
                        to_present_map[index] = doc;
                    }
                    index++;
                }
            }

            Log.Info("Show similar documents");
            index = 0;
            using (var output = new StreamWriter("out.txt"))
            {
                foreach (var e in to_present)
                {
                    output.WriteLine($"#{index++}: {e.Item1}");
                    output.WriteLine("-------------1--------------");
                    output.WriteLine(to_present_map[e.Item2].Item1);
                    output.WriteLine(to_present_map[e.Item2].Item2);
                    output.WriteLine("-------------2--------------");
                    output.WriteLine(to_present_map[e.Item3].Item1);
                    output.WriteLine(to_present_map[e.Item3].Item2);
                    output.WriteLine("#############################");
                    output.WriteLine();
                }
            }

            Console.WriteLine("Completed");
            Console.ReadKey();
        }

        private static IEnumerable<string> ExtractLemmas(string text)
        {
            return
                _lexer.ExtractUniqueLexTokensWithoutStopWords(text)
                .Select(t => t.Token)
                .Where(s => s.Any(c => char.IsLetter(c)));
        }
    }
}
