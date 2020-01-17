using Accord.MachineLearning;
using Lemmatization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZeroLevel;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Semantic.Helpers;

namespace TFIDFbee
{
    class Program
    {
        private const string source = @"D:\Desktop\lenta-ru-data-set_19990901_20171204.json";
        private readonly static ILexProvider _lexer = new LexProvider(new LemmaLexer());

        static void Main(string[] args)
        {
            Configuration.Save(Configuration.ReadFromApplicationConfig());
            /*var codebook = new TFIDF()
            {
                Tf = TermFrequency.Log,
                Idf = InverseDocumentFrequency.Default,
                UpdateDictionary = true
            };*/
            var codebook = new ZeroLevel.Services.Semantic.Helpers.BagOfWords();
            foreach (var batch in ParseBatches(1000))
            {
                codebook.Learn(batch);
                Console.WriteLine($"Documents: {codebook.NumberOfDocuments}");
                Console.WriteLine($"Words: {codebook.NumberOfWords}");
            }

            var vectors = new List<SparceVector>();
            foreach (var docs in ReadRawDocumentBatches(1000))
            {
                foreach (var doc in docs)
                {
                    var words = _lexer.ExtractLexTokens(doc.Item2).Select(t => t.Token)/*.Concat(_lexer.ExtractLexTokens(doc.Text).Select(t => t.Token))*/.ToArray();
                    vectors.Add(codebook.Transform(words));
                }
            }

            var list = new List<Tuple<double, int, int>>();
            for (int i = 0; i < vectors.Count; i++)
            {
                for (int j = i + 1; j < vectors.Count - 1; j++)
                {
                    if (i == j) continue;
                    var diff = vectors[i].Measure(vectors[j]);
                    if (diff > double.Epsilon)
                    {
                        list.Add(Tuple.Create(diff, i, j));
                    }
                }
            }

            var to_present = list.OrderBy(e => e.Item1).Take(200).ToArray();
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
            foreach (var docs in ReadRawDocumentBatches(1000))
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

            index = 0;
            foreach (var e in to_present)
            {
                Console.WriteLine($"#{index++}: {e.Item1}");
                Console.WriteLine(to_present_map[e.Item2].Item1);
                Console.WriteLine(to_present_map[e.Item3].Item2);
                Console.WriteLine("--------------------");
                Console.WriteLine();
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

        public static IEnumerable<string[][]> ReadBatches(int size)
        {
            var list = new List<string[]>();
            foreach (var batch in ReadDocumentBatches(size))
            {
                yield return batch.ToArray();
                list.Clear();
            }
        }

        public static IEnumerable<IEnumerable<string[]>> ReadDocumentBatches(int size)
        {
            string line;
            var batch = new List<string[]>();
            string title = null;
            string text = null;
            using (StreamReader reader = new StreamReader(source))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    var titleIndex = line.IndexOf("\"metaTitle\":");
                    if (titleIndex >= 0)
                    {
                        var start = line.IndexOf("\"", titleIndex + 12);
                        var end = line.LastIndexOf("\"");
                        if (start < end && start != -1 && end != -1)
                        {
                            title = line.Substring(start + 1, end - start - 1);
                        }
                    }
                    else
                    {
                        var textIndex = line.IndexOf("\"plaintext\":");
                        if (textIndex >= 0 && title != null)
                        {
                            var start = line.IndexOf("\"", textIndex + 12);
                            var end = line.LastIndexOf("\"");
                            if (start < end && start != -1 && end != -1)
                            {
                                text = line.Substring(start + 1, end - start - 1);
                                batch.Add(ExtractLemmas(title).Concat(ExtractLemmas(text)).ToArray());
                                if (batch.Count >= size)
                                {
                                    yield return batch;
                                    batch.Clear();
                                    GC.Collect(2);
                                }
                                title = null;
                                text = null;
                            }
                        }
                    }
                }
            }
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        public static IEnumerable<IEnumerable<Tuple<string, string>>> ReadRawDocumentBatches(int size)
        {
            string line;
            var batch = new List<Tuple<string, string>>();
            string title = null;
            string text = null;
            using (StreamReader reader = new StreamReader(source))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    var titleIndex = line.IndexOf("\"metaTitle\":");
                    if (titleIndex >= 0)
                    {
                        var start = line.IndexOf("\"", titleIndex + 12);
                        var end = line.LastIndexOf("\"");
                        if (start < end && start != -1 && end != -1)
                        {
                            title = line.Substring(start + 1, end - start - 1);
                        }
                    }
                    else
                    {
                        var textIndex = line.IndexOf("\"plaintext\":");
                        if (textIndex >= 0 && title != null)
                        {
                            var start = line.IndexOf("\"", textIndex + 12);
                            var end = line.LastIndexOf("\"");
                            if (start < end && start != -1 && end != -1)
                            {
                                text = line.Substring(start + 1, end - start - 1);
                                batch.Add(Tuple.Create(title, text));
                                if (batch.Count >= size)
                                {
                                    yield return batch;
                                    batch.Clear();
                                    GC.Collect(2);
                                }
                                title = null;
                                text = null;
                            }
                        }
                    }
                }
            }
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        private class RecordParser
        {
            private enum RPState
            {
                WaitKey,
                ParseKey,
                WaitKeyConfirm,
                WaitValue,
                ParseValue
            }
            private readonly StringBuilder _builder = new StringBuilder();
            private RPState State = RPState.WaitKey;
            private char _previous = '\0';
            private string _key;
            private string _value;
            private readonly Action<string, string> _callback;

            public RecordParser(Action<string, string> callback)
            {
                _callback = callback;
            }

            public void Append(string text)
            {
                foreach (var ch in text)
                {
                    switch (State)
                    {
                        case RPState.WaitKey:
                            if (ch.Equals('"'))
                            {
                                State = RPState.ParseKey;
                                _builder.Clear();
                            }
                            break;
                        case RPState.ParseKey:
                            if (ch.Equals('"') && _previous != '\\')
                            {
                                if (_builder.Length > 0)
                                {
                                    State = RPState.WaitKeyConfirm;
                                }
                                else
                                {
                                    State = RPState.WaitKey;
                                }
                            }
                            else
                            {
                                _builder.Append(ch);
                            }
                            break;
                        case RPState.WaitKeyConfirm:
                            if (ch.Equals(':'))
                            {
                                _key = _builder.ToString();
                                State = RPState.WaitValue;
                            }
                            else if (ch == ' ' || ch == '\r' || ch == '\n')
                            {
                                // nothing
                            }
                            else
                            {
                                State = RPState.WaitKey;
                            }
                            break;
                        case RPState.WaitValue:
                            if (ch.Equals('"'))
                            {
                                State = RPState.ParseValue;
                                _builder.Clear();
                            }
                            else if (ch == ' ' || ch == '\r' || ch == '\n')
                            {
                                // nothing
                            }
                            else
                            {
                                State = RPState.WaitKey;
                            }
                            break;
                        case RPState.ParseValue:
                            if (ch.Equals('"') && _previous != '\\')
                            {
                                if (_builder.Length > 0)
                                {
                                    _value = _builder.ToString();
                                    _callback(_key, _value);
                                }
                                State = RPState.WaitKey;
                            }
                            else
                            {
                                _builder.Append(ch);
                            }
                            break;
                    }
                    _previous = ch;
                }
            }
        }

        public static IEnumerable<string[][]> ParseBatches(int size)
        {
            var list = new List<string[]>();
            foreach (var record in Parse())
            {
                list.Add(record);
                if (list.Count > size)
                {
                    yield return list.ToArray();
                    list.Clear();
                }
            }
            if (list.Count > 0)
            {
                yield return list.ToArray();
            }
        }

        public static IEnumerable<string[]> Parse()
        {
            var result = new string[2];
            var parser = new RecordParser((k, v) =>
            {
                switch (k)
                {
                    case "metaTitle": result[0] = v; break;
                    case "plaintext": result[1] = v; break;
                }
            });
            char[] buffer = new char[16536];
            int count = 0;
            using (StreamReader reader = new StreamReader(source))
            {
                count = reader.Read(buffer, 0, buffer.Length);
                parser.Append(new string(buffer, 0, count));

                if (!string.IsNullOrEmpty(result[0]) && !string.IsNullOrEmpty(result[1]))
                {
                    yield return result;
                    result[0] = null;
                    result[1] = null;
                }
            }
        }
    }
}
