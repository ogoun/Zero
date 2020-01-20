using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Semantic.Helpers;

namespace TFIDFbee.Reader
{
    public class JsonByLineReader
        : IDocumentReader
    {
        private readonly string _file;
        private readonly ILexProvider _lexer;

        public JsonByLineReader(string file, ILexProvider lexer)
        {
            _file = file;
            _lexer = lexer;
        }

        public IEnumerable<IEnumerable<BagOfTerms>> ReadBatches(int size)
        {
            string line;
            var batch = new List<BagOfTerms>();
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
                                batch.Add(new BagOfTerms(title + " " + text, _lexer));
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
