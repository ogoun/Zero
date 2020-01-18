using System;
using System.Collections.Generic;

namespace TFIDFbee.Reader
{
    public interface IDocumentReader
    {
        IEnumerable<string[][]> ReadBatches(int size);
        public IEnumerable<IEnumerable<Tuple<string, string>>> ReadRawDocumentBatches(int size);
    }
}
