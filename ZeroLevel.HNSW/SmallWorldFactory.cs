using System.IO;

namespace ZeroLevel.HNSW
{
    public static class SmallWorld
    {
        public static SmallWorld<TItem> CreateWorld<TItem>(NSWOptions<TItem> options) 
            => new SmallWorld<TItem>(options);
        public static SmallWorld<TItem> CreateWorldFrom<TItem>(NSWOptions<TItem> options, Stream stream) 
            => new SmallWorld<TItem>(options, stream);
    }
}
