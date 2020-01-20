using System.Collections.Generic;
using ZeroLevel.Services.Semantic.Helpers;

namespace TFIDFbee.Reader
{
    public interface IDocumentReader
    {
        IEnumerable<IEnumerable<BagOfTerms>> ReadBatches(int size);
    }
}
