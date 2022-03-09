﻿using MemoryPools.Memory.Pooling;
using System;
using System.Runtime.CompilerServices;

/*https://github.com/sidristij/memory-pools*/

namespace MemoryPools.Memory
{
    internal sealed class InternalArraysPool
	{
		private const int MinBufferSize = 128;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CountdownMemoryOwner<byte> Rent(int length)
		{
			return Rent<byte>(length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CountdownMemoryOwner<T> Rent<T>(int length, bool noDefaultOwner = false)
		{
			var realLength = length;
			var allocLength = length > MinBufferSize ? length : MinBufferSize;
			var owner = BucketsBasedCrossThreadsMemoryPool<T>.Shared.Rent(allocLength);
			return owner.AsCountdown(0, realLength, noDefaultOwner);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CountdownMemoryOwner<T> RentFrom<T>(ReadOnlySpan<T> source, bool noDefaultOwner = false)
		{
			var mem = Rent<T>(source.Length, noDefaultOwner);
			source.CopyTo(mem.Memory.Span);
			return mem;
		}
	}
}
