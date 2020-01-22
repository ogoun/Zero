using Lemmatization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TFIDFbee.Reader;
using ZeroLevel.Services.Semantic;

namespace TFIDFbee
{
    class Program
    {
        private const string source = @"D:\Desktop\lenta-ru-data-set_19990901_20171204_limit_1000.json";
        private readonly static ILexProvider _lexer = new LexProvider(new LemmaLexer());
        private readonly static ConcurrentDictionary<string, double> _scoring = new ConcurrentDictionary<string, double>();

        static void Main(string[] args)
        {
            IDF idf = new IDF();
            IDocumentReader reader = new JsonByLineReader(source, _lexer);
            foreach (var batch in reader.ReadBatches(1000))
            {
                foreach (var doc in batch)
                {
                    idf.Append(doc);
                }
            }
            foreach (var batch in reader.ReadBatches(1000))
            {
                foreach (var doc in batch)
                {
                    var tfidf = TFIDF.TfIdf(doc, idf);
                    Console.WriteLine(String.Join(" ", tfidf.OrderByDescending(p => p.Value).Take(10).Select(p => p.Key)));
                    Console.WriteLine();
                    Console.WriteLine("                                     ***");
                    Console.WriteLine();
                    Thread.Sleep(1000);
                }
            }


            /*
            Log.AddConsoleLogger(ZeroLevel.Logging.LogLevel.FullDebug);
            Configuration.Save(Configuration.ReadFromApplicationConfig());
            IDocumentReader reader = new JsonByLineReader(source, s => ExtractLemmas(s));

            ZeroLevel.Services.Semantic.Helpers.BagOfTerms codebook;
            if (File.Exists("model.bin"))
            {
                Log.Info("Load model from file");
                using (var stream = new FileStream("model.bin", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    codebook = MessageSerializer.Deserialize<ZeroLevel.Services.Semantic.Helpers.BagOfTerms>(stream);
                }
            }
            else
            {
                Log.Info("Create and train model");
                codebook = new ZeroLevel.Services.Semantic.Helpers.BagOfTerms();
                foreach (var batch in reader.ReadBatches(1000))
                {
                    codebook.Learn(batch);
                    Log.Info($"\r\n\tDocuments: {codebook.NumberOfDocuments}\r\n\tWords: {codebook.NumberOfWords}");
                }
                using (var stream = new FileStream("model.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    MessageSerializer.Serialize<ZeroLevel.Services.Semantic.Helpers.BagOfTerms>(stream, codebook);
                }
            }


            Log.Info("Create vectors");

            foreach (var docs in reader.ReadRawDocumentBatches(1000))
            {
                foreach (var doc in docs)
                {
                    var words = ExtractLemmas(doc.Item2).Concat(ExtractLemmas(doc.Item1)).Distinct().ToArray();
                    var vector = codebook.Transform(words);
                    for (var i = 0; i< words.Length; i++)
                    {
                        var word = words[i];
                        if (false == _scoring.ContainsKey(word))
                        {
                            _scoring.TryAdd(word, vector)
                        }
                    }
                }
            }
            using (var stream = new FileStream("vectors.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                MessageSerializer.SerializeCompatible<List<SparceVector>>(stream, vectors);
            }


            Log.Info("Find similar documents");
            var list = new List<Tuple<double, int, int>>();
            long total_count = ((long)vectors.Count * (long)vectors.Count);
            long count = 0;
            double d = (double.Epsilon * 2.0d);
            for (int i = 0; i < vectors.Count; i++)
            {
                for (int j = i + 1; j < vectors.Count - 1; j++)
                {
                    count++;
                    if (count % 10000000 == 0)
                    {
                        Log.Info($"Progress: {((count * 100.0d) / total_count)} %.\tFound similars: {list.Count}.");
                    }
                    if (i == j) continue;
                    var diff = vectors[i].Measure(vectors[j]);
                    if (diff > d && diff < 0.0009d)
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
            */
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
