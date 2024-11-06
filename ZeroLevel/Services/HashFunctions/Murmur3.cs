using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZeroLevel.Services.HashFunctions
{
    public static class Murmur3
    {
        // 128 bit output, 64 bit platform version

        public static ulong READ_SIZE = 16;
        private static ulong C1 = 0x87c37b91114253d5L;
        private static ulong C2 = 0x4cf5ad432745937fL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MixKey1(ulong k1)
        {
            k1 *= C1;
            k1 = k1.RotateLeft(31);
            k1 *= C2;
            return k1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MixKey2(ulong k2)
        {
            k2 *= C2;
            k2 = k2.RotateLeft(33);
            k2 *= C1;
            return k2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MixFinal(ulong k)
        {
            // avalanche bits

            k ^= k >> 33;
            k *= 0xff51afd7ed558ccdL;
            k ^= k >> 33;
            k *= 0xc4ceb9fe1a85ec53L;
            k ^= k >> 33;
            return k;
        }

        public static byte[] ComputeHash(byte[] bb, ulong seed = 0)
        {
            var h1 = seed;
            ulong h2 = 0;
            var length = 0UL;
            int pos = 0;
            ulong remaining = (ulong)bb.Length;
            // read 128 bits, 16 bytes, 2 longs in eacy cycle
            while (remaining >= READ_SIZE)
            {
                ulong k1 = bb.GetUInt64(pos);
                pos += 8;
                ulong k2 = bb.GetUInt64(pos);
                pos += 8;
                length += READ_SIZE;
                remaining -= READ_SIZE;

                h1 ^= MixKey1(k1);
                h1 = h1.RotateLeft(27);
                h1 += h2;
                h1 = h1 * 5 + 0x52dce729;
                h2 ^= MixKey2(k2);
                h2 = h2.RotateLeft(31);
                h2 += h1;
                h2 = h2 * 5 + 0x38495ab5;
            }

            // if the input MOD 16 != 0
            if (remaining > 0)
            {
                ulong k1 = 0;
                ulong k2 = 0;
                length += remaining;
                // little endian (x86) processing
                switch (remaining)
                {
                    case 15:
                        k2 ^= (ulong)bb[pos + 14] << 48; // fall through
                        goto case 14;
                    case 14:
                        k2 ^= (ulong)bb[pos + 13] << 40; // fall through
                        goto case 13;
                    case 13:
                        k2 ^= (ulong)bb[pos + 12] << 32; // fall through
                        goto case 12;
                    case 12:
                        k2 ^= (ulong)bb[pos + 11] << 24; // fall through
                        goto case 11;
                    case 11:
                        k2 ^= (ulong)bb[pos + 10] << 16; // fall through
                        goto case 10;
                    case 10:
                        k2 ^= (ulong)bb[pos + 9] << 8; // fall through
                        goto case 9;
                    case 9:
                        k2 ^= (ulong)bb[pos + 8]; // fall through
                        goto case 8;
                    case 8:
                        k1 ^= bb.GetUInt64(pos);
                        break;
                    case 7:
                        k1 ^= (ulong)bb[pos + 6] << 48; // fall through
                        goto case 6;
                    case 6:
                        k1 ^= (ulong)bb[pos + 5] << 40; // fall through
                        goto case 5;
                    case 5:
                        k1 ^= (ulong)bb[pos + 4] << 32; // fall through
                        goto case 4;
                    case 4:
                        k1 ^= (ulong)bb[pos + 3] << 24; // fall through
                        goto case 3;
                    case 3:
                        k1 ^= (ulong)bb[pos + 2] << 16; // fall through
                        goto case 2;
                    case 2:
                        k1 ^= (ulong)bb[pos + 1] << 8; // fall through
                        goto case 1;
                    case 1:
                        k1 ^= (ulong)bb[pos]; // fall through
                        break;
                    default:
                        throw new Exception("Something went wrong with remaining bytes calculation.");
                }
                h1 ^= MixKey1(k1);
                h2 ^= MixKey2(k2);
            }


            h1 ^= length;
            h2 ^= length;

            h1 += h2;
            h2 += h1;

            h1 = Murmur3.MixFinal(h1);
            h2 = Murmur3.MixFinal(h2);

            h1 += h2;
            h2 += h1;

            var hash = new byte[Murmur3.READ_SIZE];
            Array.Copy(BitConverter.GetBytes(h1), 0, hash, 0, 8);
            Array.Copy(BitConverter.GetBytes(h2), 0, hash, 8, 8);
            return hash;
        }

        public static ulong ComputeULongHash(byte[] bb, ulong seed = 0)
        {
            var h1 = seed;
            ulong h2 = 0;
            var length = 0UL;
            int pos = 0;
            ulong remaining = (ulong)bb.Length;
            // read 128 bits, 16 bytes, 2 longs in eacy cycle
            while (remaining >= READ_SIZE)
            {
                ulong k1 = bb.GetUInt64(pos);
                pos += 8;
                ulong k2 = bb.GetUInt64(pos);
                pos += 8;
                length += READ_SIZE;
                remaining -= READ_SIZE;

                h1 ^= MixKey1(k1);
                h1 = h1.RotateLeft(27);
                h1 += h2;
                h1 = h1 * 5 + 0x52dce729;
                h2 ^= MixKey2(k2);
                h2 = h2.RotateLeft(31);
                h2 += h1;
                h2 = h2 * 5 + 0x38495ab5;
            }

            // if the input MOD 16 != 0
            if (remaining > 0)
            {
                ulong k1 = 0;
                ulong k2 = 0;
                length += remaining;
                // little endian (x86) processing
                switch (remaining)
                {
                    case 15:
                        k2 ^= (ulong)bb[pos + 14] << 48; // fall through
                        goto case 14;
                    case 14:
                        k2 ^= (ulong)bb[pos + 13] << 40; // fall through
                        goto case 13;
                    case 13:
                        k2 ^= (ulong)bb[pos + 12] << 32; // fall through
                        goto case 12;
                    case 12:
                        k2 ^= (ulong)bb[pos + 11] << 24; // fall through
                        goto case 11;
                    case 11:
                        k2 ^= (ulong)bb[pos + 10] << 16; // fall through
                        goto case 10;
                    case 10:
                        k2 ^= (ulong)bb[pos + 9] << 8; // fall through
                        goto case 9;
                    case 9:
                        k2 ^= (ulong)bb[pos + 8]; // fall through
                        goto case 8;
                    case 8:
                        k1 ^= bb.GetUInt64(pos);
                        break;
                    case 7:
                        k1 ^= (ulong)bb[pos + 6] << 48; // fall through
                        goto case 6;
                    case 6:
                        k1 ^= (ulong)bb[pos + 5] << 40; // fall through
                        goto case 5;
                    case 5:
                        k1 ^= (ulong)bb[pos + 4] << 32; // fall through
                        goto case 4;
                    case 4:
                        k1 ^= (ulong)bb[pos + 3] << 24; // fall through
                        goto case 3;
                    case 3:
                        k1 ^= (ulong)bb[pos + 2] << 16; // fall through
                        goto case 2;
                    case 2:
                        k1 ^= (ulong)bb[pos + 1] << 8; // fall through
                        goto case 1;
                    case 1:
                        k1 ^= (ulong)bb[pos]; // fall through
                        break;
                    default:
                        throw new Exception("Something went wrong with remaining bytes calculation.");
                }
                h1 ^= MixKey1(k1);
                h2 ^= MixKey2(k2);
            }


            h1 ^= length;
            h2 ^= length;

            h1 += h2;
            h2 += h1;

            h1 = Murmur3.MixFinal(h1);
            h2 = Murmur3.MixFinal(h2);

            h1 += h2;
            h2 += h1;

            return h2;
        }

        /// <summary>
        /// Hashes the <paramref name="bytes"/> into a MurmurHash3 as a <see cref="uint"/>.
        /// </summary>
        /// <param name="bytes">The span.</param>
        /// <param name="seed">The seed for this algorithm.</param>
        /// <returns>The MurmurHash3 as a <see cref="uint"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ComputeUIntHash(ref ReadOnlySpan<byte> bytes, uint seed)
        {
            ref byte bp = ref MemoryMarshal.GetReference(bytes);
            ref uint endPoint = ref Unsafe.Add(ref Unsafe.As<byte, uint>(ref bp), bytes.Length >> 2);
            if (bytes.Length >= 4)
            {
                do
                {
                    seed = RotateLeft(seed ^ RotateLeft(Unsafe.ReadUnaligned<uint>(ref bp) * 3432918353U, 15) * 461845907U, 13) * 5 - 430675100;
                    bp = ref Unsafe.Add(ref bp, 4);
                } while (Unsafe.IsAddressLessThan(ref Unsafe.As<byte, uint>(ref bp), ref endPoint));
            }
            var remainder = bytes.Length & 3;
            if (remainder > 0)
            {
                uint num = 0;
                if (remainder > 2) num ^= Unsafe.Add(ref endPoint, 2) << 16;
                if (remainder > 1) num ^= Unsafe.Add(ref endPoint, 1) << 8;
                num ^= endPoint;

                seed ^= RotateLeft(num * 3432918353U, 15) * 461845907U;
            }
            seed ^= (uint)bytes.Length;
            seed = (uint)((seed ^ (seed >> 16)) * -2048144789);
            seed = (uint)((seed ^ (seed >> 13)) * -1028477387);
            return seed ^ seed >> 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateLeft(this ulong original, int bits) => (original << bits) | (original >> (64 - bits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(this uint original, int bits) => (original << bits) | (original >> (32 - bits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateRight(this ulong original, int bits) =>
            (original >> bits) | (original << (64 - bits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetUInt64(this byte[] bb, int pos) =>
            (ulong)(bb[pos++] | bb[pos++] << 8 | bb[pos++] << 16 | bb[pos++] << 24);

        /// <summary>
        /// A 32-bit murmur3 implementation.
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compute(int h)
        {
            uint a = (uint)h;
            a ^= a >> 16;
            a *= 0x85ebca6b;
            a ^= a >> 13;
            a *= 0xc2b2ae35;
            a ^= a >> 16;
            return (int)a;
        }
    }
}
