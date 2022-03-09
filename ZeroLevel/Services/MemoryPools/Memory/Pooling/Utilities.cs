using System.Runtime.CompilerServices;

/*https://github.com/sidristij/memory-pools*/

namespace MemoryPools.Memory.Pooling
{
    internal static class Utilities
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetMaxSizeForBucket(int binIndex) => 16 << binIndex;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetBucket(int size)
		{
			if (size == 128 /*default chunk size*/) return 7;
			size--;
			var length = 0;
			while (size >= 16)
			{
				length++;
				size = size >> 1;
			}
			return length;
		}
	}
}
