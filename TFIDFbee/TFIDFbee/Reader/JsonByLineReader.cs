using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TFIDFbee.Reader
{
    public class JsonByLineReader
        : IDocumentReader
    {
        private readonly string _file;
        private readonly Func<string, IEnumerable<string>> _lexer;

        public JsonByLineReader(string file, Func<string, IEnumerable<string>> lexer)
        {
            _file = file;
            _lexer = lexer;
        }

        public IEnumerable<string[][]> ReadBatches(int size)
        {
            var list = new List<string[]>();
            foreach (var batch in ReadDocumentBatches(size))
            {
                yield return batch.ToArray();
                list.Clear();
            }
        }

        private IEnumerable<IEnumerable<string[]>> ReadDocumentBatches(int size)
        {
            string line;
            var batch = new List<string[]>();
            string title = null;
            string text = null;
            using (StreamReader reader = new StreamReader(_file))
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
                                batch.Add(_lexer(title).Concat(_lexer(text)).ToArray());
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

        public IEnumerable<IEnumerable<Tuple<string, string>>> ReadRawDocumentBatches(int size)
        {
            string line;
            var batch = new List<Tuple<string, string>>();
            string title = null;
            string text = null;
            using (StreamReader reader = new StreamReader(_file))
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
    }
}
