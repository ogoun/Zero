using System.Collections.Generic;
using System.IO;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Semantic.Helpers;

namespace TFIDFbee.Reader
{
    public class StateMachineReader
        : IDocumentReader
    {
        private readonly string _file;
        private readonly ILexProvider _lexer;

        public StateMachineReader(string file, ILexProvider lexer)
        {
            _file = file;
            _lexer = lexer;
        }

        private IEnumerable<string[]> Parse()
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
            using (StreamReader reader = new StreamReader(_file))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
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

        public IEnumerable<IEnumerable<BagOfTerms>> ReadBatches(int size)
        {
            var list = new List<BagOfTerms>();
            foreach (var record in Parse())
            {
                list.Add(new BagOfTerms(record[0] + " " + record[1], _lexer));
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
    }
}
