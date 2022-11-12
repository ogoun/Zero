namespace PartitionFileStorageTest
{
    public struct MsisdnParts
    {
        public int FirstDigit { get; }
        public int OtherDigits { get; }

        public MsisdnParts(int firstDigit, int otherDigits)
        {
            FirstDigit = firstDigit;
            OtherDigits = otherDigits;
        }

        public override string ToString() => $"({FirstDigit},{OtherDigits})";
    }
    public static class MsisdnHelper
    {
        public static MsisdnParts SplitParts(this ulong msisdn)
        {
            //расчитываем только на номера российской нумерации ("7" и 10 цифр)
            //это числа от 70_000_000_000 до 79_999_999_999

            if (msisdn < 70_000_000_000 || msisdn > 79_999_999_999) throw new ArgumentException(nameof(msisdn));

            var firstDigit = (int)((msisdn / 1_000_000_000L) % 10);
            var otherDigits = (int)(msisdn % 1_000_000_000L);

            return new MsisdnParts(firstDigit, otherDigits);
        }

        public static ulong CombineParts(int firstDigit, int otherDigits)
        {
            return (ulong)(70_000_000_000L + firstDigit * 1_000_000_000L + otherDigits);
        }

        public static IEnumerable<ulong> ParseMsisdns(this IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                ulong msisdn;
                if (line.TryParseMsisdn(out msisdn))
                {
                    yield return msisdn;
                }
            }
        }

        /// <summary>
        /// возвращаются только номера российской нумерации ("7" и 10 цифр) в виде long
        /// </summary>
        /// <param name="source"></param>
        /// <param name="msisdn"></param>
        /// <returns></returns>
        public static bool TryParseMsisdn(this string source, out ulong msisdn)
        {
            var line = source.Trim();
            var length = line.Length;

            msisdn = 0;

            //допустимы форматы номеров "+71234567890", "71234567890", "1234567890"
            if (length < 10 || length > 12) return false;

            var start = 0;
            if (length == 12) //"+71234567890"
            {
                if (line[0] != '+' || line[1] != '7') return false;
                start = 2;
            }
            if (length == 11) //"71234567890" и "81234567890"
            {
                if (line[0] != '7') return false;
                start = 1;
            }
            /*
            else if (length == 10) //"1234567890"
            {
                start = 0;
            }
            */

            ulong number = 7;

            for (var i = start; i < length; i++)
            {
                var c = line[i];
                if ('0' <= c && c <= '9')
                {
                    number = number * 10 + (ulong)(c - '0');
                }
                else
                {
                    return false;
                }
            }

            msisdn = number;
            return true;
        }
    }
}
