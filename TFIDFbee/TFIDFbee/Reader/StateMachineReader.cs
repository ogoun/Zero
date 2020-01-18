using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TFIDFbee.Reader
{
    public class StateMachineReader
        : IDocumentReader
    {
        private readonly string _file;
        private readonly Func<string, IEnumerable<string>> _lexer;

        public StateMachineReader(string file, Func<string, IEnumerable<string>> lexer)
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

        public IEnumerable<string[][]> ReadBatches(int size)
        {
            var list = new List<string[]>();
            foreach (var record in Parse())
            {
                list.Add((_lexer(record[0]).Concat(_lexer(record[1])).ToArray()));
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

        public IEnumerable<IEnumerable<Tuple<string, string>>> ReadRawDocumentBatches(int size)
        {
            var list = new List<Tuple<string, string>>();
            foreach (var record in Parse())
            {
                list.Add(Tuple.Create(record[0], record[1]));
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
