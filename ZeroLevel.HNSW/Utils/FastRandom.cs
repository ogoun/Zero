﻿using System;
using System.Runtime.CompilerServices;

namespace ZeroLevel.HNSW
{
    public sealed class DefaultRandomGenerator
    {
        /// <summary>
        /// This is the default configuration (it supports the optimization process to be executed on multiple threads)
        /// </summary>
        public static DefaultRandomGenerator Instance { get; } = new DefaultRandomGenerator(allowParallel: true);

        /// <summary>
        /// This uses the same random number generator but forces the optimization process to run on a single thread (which may be desirable if multiple requests may be processed concurrently
        /// or if it is otherwise not desirable to let a single request access all of the CPUs)
        /// </summary>
        public static DefaultRandomGenerator DisableThreading { get; } = new DefaultRandomGenerator(allowParallel: false);

        private DefaultRandomGenerator(bool allowParallel) => IsThreadSafe = allowParallel;

        public bool IsThreadSafe { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int minValue, int maxValue) => ThreadSafeFastRandom.Next(minValue, maxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat() => ThreadSafeFastRandom.NextFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NextFloats(Span<float> buffer) => ThreadSafeFastRandom.NextFloats(buffer);
    }

    internal static class ThreadSafeFastRandom
    {
        private static readonly Random _global = new Random();

        [ThreadStatic]
        private static FastRandom _local;

        private static int GetGlobalSeed()
        {
            int seed;
            lock (_global)
            {
                seed = _global.Next();
            }
            return seed;
        }

        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than System.Int32.MaxValue.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next()
        {
            var inst = _local;
            if (inst == null)
            {
                int seed;
                seed = GetGlobalSeed();
                _local = inst = new FastRandom(seed);
            }
            return inst.Next();
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. maxValue must be greater than or equal to 0.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than maxValue; that is, the range of return values ordinarily includes 0 but not maxValue. However,
        //  if maxValue equals 0, maxValue is returned.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next(int maxValue)
        {
            var inst = _local;
            if (inst == null)
            {
                int seed;
                seed = GetGlobalSeed();
                _local = inst = new FastRandom(seed);
            }
            int ans;
            do
            {
                ans = inst.Next(maxValue);
            } while (ans == maxValue);

            return ans;
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue; that is, the range of return values includes minValue but not maxValue. If minValue
        //  equals maxValue, minValue is returned.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next(int minValue, int maxValue)
        {
            var inst = _local;
            if (inst == null)
            {
                int seed;
                seed = GetGlobalSeed();
                _local = inst = new FastRandom(seed);
            }
            return inst.Next(minValue, maxValue);
        }

        /// <summary>
        /// Generates a random float. Values returned are from 0.0 up to but not including 1.0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextFloat()
        {
            var inst = _local;
            if (inst == null)
            {
                int seed;
                seed = GetGlobalSeed();
                _local = inst = new FastRandom(seed);
            }
            return inst.NextFloat();
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NextFloats(Span<float> buffer)
        {
            var inst = _local;
            if (inst == null)
            {
                int seed;
                seed = GetGlobalSeed();
                _local = inst = new FastRandom(seed);
            }
            inst.NextFloats(buffer);
        }
    }

    /// <summary>
    /// A fast random number generator for .NET, from https://www.codeproject.com/Articles/9187/A-fast-equivalent-for-System-Random
    /// Colin Green, January 2005
    ///
    /// September 4th 2005
    ///	 Added NextBytesUnsafe() - commented out by default.
    ///	 Fixed bug in Reinitialise() - y,z and w variables were not being reset.
    ///
    /// Key points:
    ///  1) Based on a simple and fast xor-shift pseudo random number generator (RNG) specified in:
    ///  Marsaglia, George. (2003). Xorshift RNGs.
    ///  http://www.jstatsoft.org/v08/i14/xorshift.pdf
    ///
    ///  This particular implementation of xorshift has a period of 2^128-1. See the above paper to see
    ///  how this can be easily extened if you need a longer period. At the time of writing I could find no
    ///  information on the period of System.Random for comparison.
    ///
    ///  2) Faster than System.Random. Up to 8x faster, depending on which methods are called.
    ///
    ///  3) Direct replacement for System.Random. This class implements all of the methods that System.Random
    ///  does plus some additional methods. The like named methods are functionally equivalent.
    ///
    ///  4) Allows fast re-initialisation with a seed, unlike System.Random which accepts a seed at construction
    ///  time which then executes a relatively expensive initialisation routine. This provides a vast speed improvement
    ///  if you need to reset the pseudo-random number sequence many times, e.g. if you want to re-generate the same
    ///  sequence many times. An alternative might be to cache random numbers in an array, but that approach is limited
    ///  by memory capacity and the fact that you may also want a large number of different sequences cached. Each sequence
    ///  can each be represented by a single seed value (int) when using FastRandom.
    ///
    ///  Notes.
    ///  A further performance improvement can be obtained by declaring local variables as static, thus avoiding
    ///  re-allocation of variables on each call. However care should be taken if multiple instances of
    ///  FastRandom are in use or if being used in a multi-threaded environment.
    ///
    /// </summary>
    internal class FastRandom
    {
        // The +1 ensures NextDouble doesn't generate 1.0
        const float FLOAT_UNIT_INT = 1.0f / ((float)int.MaxValue + 1.0f);

        const double REAL_UNIT_INT = 1.0 / ((double)int.MaxValue + 1.0);
        const double REAL_UNIT_UINT = 1.0 / ((double)uint.MaxValue + 1.0);
        const uint Y = 842502087, Z = 3579807591, W = 273326509;

        uint x, y, z, w;

        /// <summary>
        /// Initialises a new instance using time dependent seed.
        /// </summary>
        public FastRandom()
        {
            // Initialise using the system tick count.
            Reinitialise(Environment.TickCount);
        }

        /// <summary>
        /// Initialises a new instance using an int value as seed.
        /// This constructor signature is provided to maintain compatibility with
        /// System.Random
        /// </summary>
        public FastRandom(int seed)
        {
            Reinitialise(seed);
        }

        /// <summary>
        /// Reinitialises using an int value as a seed.
        /// </summary>
        public void Reinitialise(int seed)
        {
            // The only stipulation stated for the xorshift RNG is that at least one of
            // the seeds x,y,z,w is non-zero. We fulfill that requirement by only allowing
            // resetting of the x seed
            x = (uint)seed;
            y = Y;
            z = Z;
            w = W;
        }

        /// <summary>
        /// Generates a random int over the range 0 to int.MaxValue-1.
        /// MaxValue is not generated in order to remain functionally equivalent to System.Random.Next().
        /// This does slightly eat into some of the performance gain over System.Random, but not much.
        /// For better performance see:
        ///
        /// Call NextInt() for an int over the range 0 to int.MaxValue.
        ///
        /// Call NextUInt() and cast the result to an int to generate an int over the full Int32 value range
        /// including negative values.
        /// </summary>
        public int Next()
        {
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

            // Handle the special case where the value int.MaxValue is generated. This is outside of
            // the range of permitted values, so we therefore call Next() to try again.
            uint rtn = w & 0x7FFFFFFF;
            if (rtn == 0x7FFFFFFF)
                return Next();
            return (int)rtn;
        }

        /// <summary>
        /// Generates a random int over the range 0 to upperBound-1, and not including upperBound.
        /// </summary>
        public int Next(int upperBound)
        {
            if (upperBound < 0)
                throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >=0");

            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;

            // The explicit int cast before the first multiplication gives better performance.
            // See comments in NextDouble.
            return (int)((REAL_UNIT_INT * (int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))))) * upperBound);
        }

        /// <summary>
        /// Generates a random int over the range lowerBound to upperBound-1, and not including upperBound.
        /// upperBound must be >= lowerBound. lowerBound may be negative.
        /// </summary>
        public int Next(int lowerBound, int upperBound)
        {
            if (lowerBound > upperBound)
                throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >=lowerBound");

            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;

            // The explicit int cast before the first multiplication gives better performance.
            // See comments in NextDouble.
            int range = upperBound - lowerBound;
            if (range < 0)
            {   // If range is <0 then an overflow has occured and must resort to using long integer arithmetic instead (slower).
                // We also must use all 32 bits of precision, instead of the normal 31, which again is slower.
                return lowerBound + (int)((REAL_UNIT_UINT * (double)(w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)))) * (double)((long)upperBound - (long)lowerBound));
            }

            // 31 bits of precision will suffice if range<=int.MaxValue. This allows us to cast to an int and gain
            // a little more performance.
            return lowerBound + (int)((REAL_UNIT_INT * (double)(int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))))) * (double)range);
        }

        /// <summary>
        /// Generates a random double. Values returned are from 0.0 up to but not including 1.0.
        /// </summary>
        public double NextDouble()
        {
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;

            // Here we can gain a 2x speed improvement by generating a value that can be cast to
            // an int instead of the more easily available uint. If we then explicitly cast to an
            // int the compiler will then cast the int to a double to perform the multiplication,
            // this final cast is a lot faster than casting from a uint to a double. The extra cast
            // to an int is very fast (the allocated bits remain the same) and so the overall effect
            // of the extra cast is a significant performance improvement.
            //
            // Also note that the loss of one bit of precision is equivalent to what occurs within
            // System.Random.
            return (REAL_UNIT_INT * (int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)))));
        }

        /// <summary>
        /// Generates a random double. Values returned are from 0.0 up to but not including 1.0.
        /// </summary>
        public float NextFloat()
        {
            uint x = this.x, y = this.y, z = this.z, w = this.w;
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            var value = FLOAT_UNIT_INT * (int)(0x7FFFFFFF & w);
            this.x = x; this.y = y; this.z = z; this.w = w;
            return value;
        }

        /// <summary>
        /// Fills the provided byte array with random floats.
        /// </summary>
        public void NextFloats(Span<float> buffer)
        {
            uint x = this.x, y = this.y, z = this.z, w = this.w;
            int i = 0;
            uint t;
            for (int bound = buffer.Length; i < bound;)
            {
                t = (x ^ (x << 11));
                x = y; y = z; z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = FLOAT_UNIT_INT * (int)(0x7FFFFFFF & w);
            }

            this.x = x; this.y = y; this.z = z; this.w = w;
        }


        /// <summary>
        /// Fills the provided byte array with random bytes.
        /// This method is functionally equivalent to System.Random.NextBytes().
        /// </summary>
        public void NextBytes(byte[] buffer)
        {
            // Fill up the bulk of the buffer in chunks of 4 bytes at a time.
            uint x = this.x, y = this.y, z = this.z, w = this.w;
            int i = 0;
            uint t;
            for (int bound = buffer.Length - 3; i < bound;)
            {
                // Generate 4 bytes.
                // Increased performance is achieved by generating 4 random bytes per loop.
                // Also note that no mask needs to be applied to zero out the higher order bytes before
                // casting because the cast ignores thos bytes. Thanks to Stefan Troschütz for pointing this out.
                t = (x ^ (x << 11));
                x = y; y = z; z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w;
                buffer[i++] = (byte)(w >> 8);
                buffer[i++] = (byte)(w >> 16);
                buffer[i++] = (byte)(w >> 24);
            }

            // Fill up any remaining bytes in the buffer.
            if (i < buffer.Length)
            {
                // Generate 4 bytes.
                t = (x ^ (x << 11));
                x = y; y = z; z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w;
                if (i < buffer.Length)
                {
                    buffer[i++] = (byte)(w >> 8);
                    if (i < buffer.Length)
                    {
                        buffer[i++] = (byte)(w >> 16);
                        if (i < buffer.Length)
                        {
                            buffer[i] = (byte)(w >> 24);
                        }
                    }
                }
            }
            this.x = x; this.y = y; this.z = z; this.w = w;
        }

        /// <summary>
        /// Fills the provided byte array with random bytes.
        /// This method is functionally equivalent to System.Random.NextBytes().
        /// </summary>
        public void NextBytes(Span<byte> buffer)
        {
            // Fill up the bulk of the buffer in chunks of 4 bytes at a time.
            uint x = this.x, y = this.y, z = this.z, w = this.w;
            int i = 0;
            uint t;
            for (int bound = buffer.Length - 3; i < bound;)
            {
                // Generate 4 bytes.
                // Increased performance is achieved by generating 4 random bytes per loop.
                // Also note that no mask needs to be applied to zero out the higher order bytes before
                // casting because the cast ignores thos bytes. Thanks to Stefan Troschütz for pointing this out.
                t = (x ^ (x << 11));
                x = y; y = z; z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w;
                buffer[i++] = (byte)(w >> 8);
                buffer[i++] = (byte)(w >> 16);
                buffer[i++] = (byte)(w >> 24);
            }

            // Fill up any remaining bytes in the buffer.
            if (i < buffer.Length)
            {
                // Generate 4 bytes.
                t = (x ^ (x << 11));
                x = y; y = z; z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w;
                if (i < buffer.Length)
                {
                    buffer[i++] = (byte)(w >> 8);
                    if (i < buffer.Length)
                    {
                        buffer[i++] = (byte)(w >> 16);
                        if (i < buffer.Length)
                        {
                            buffer[i] = (byte)(w >> 24);
                        }
                    }
                }
            }
            this.x = x; this.y = y; this.z = z; this.w = w;
        }

        /// <summary>
        /// Generates a uint. Values returned are over the full range of a uint,
        /// uint.MinValue to uint.MaxValue, inclusive.
        ///
        /// This is the fastest method for generating a single random number because the underlying
        /// random number generator algorithm generates 32 random bits that can be cast directly to
        /// a uint.
        /// </summary>
        public uint NextUInt()
        {
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;
            return (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)));
        }

        /// <summary>
        /// Generates a random int over the range 0 to int.MaxValue, inclusive.
        /// This method differs from Next() only in that the range is 0 to int.MaxValue
        /// and not 0 to int.MaxValue-1.
        ///
        /// The slight difference in range means this method is slightly faster than Next()
        /// but is not functionally equivalent to System.Random.Next().
        /// </summary>
        public int NextInt()
        {
            uint t = (x ^ (x << 11));
            x = y; y = z; z = w;
            return (int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))));
        }


        // Buffer 32 bits in bitBuffer, return 1 at a time, keep track of how many have been returned
        // with bitBufferIdx.
        uint bitBuffer;
        uint bitMask = 1;

        /// <summary>
        /// Generates a single random bit.
        /// This method's performance is improved by generating 32 bits in one operation and storing them
        /// ready for future calls.
        /// </summary>
        public bool NextBool()
        {
            if (bitMask == 1)
            {
                // Generate 32 more bits.
                uint t = (x ^ (x << 11));
                x = y; y = z; z = w;
                bitBuffer = w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                // Reset the bitMask that tells us which bit to read next.
                bitMask = 0x80000000;
                return (bitBuffer & bitMask) == 0;
            }

            return (bitBuffer & (bitMask >>= 1)) == 0;
        }
    }
}
