namespace ZeroLevel
{
    public static class NumberBitsExtensions
    {
        private const int ONE_I = 1;
        private const uint ONE_UI = 1U;
        private const long ONE_L = 1L;
        private const ulong ONE_UL = 1UL;

        public static ulong SetBit(this ulong k, int position)
        {
            k |= (ONE_UL << position);
            return k;
        }

        public static ulong ResetBit(this ulong k, int position)
        {
            k &= ~(ONE_UL << position);
            return k;
        }

        public static long SetBit(this long k, int position)
        {
            k |= (ONE_L << position);
            return k;
        }

        public static long ResetBit(this long k, int position)
        {
            k &= ~(ONE_L << position);
            return k;
        }

        public static int SetBit(this int k, int position)
        {
            k |= (ONE_I << position);
            return k;
        }

        public static int ResetBit(this int k, int position)
        {
            k &= ~(ONE_I << position);
            return k;
        }

        public static uint SetBit(this uint k, int position)
        {
            k |= (ONE_UI << position);
            return k;
        }

        public static uint ResetBit(this uint k, int position)
        {
            k &= ~(ONE_UI << position);
            return k;
        }
    }
}
