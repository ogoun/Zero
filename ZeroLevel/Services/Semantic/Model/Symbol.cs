using System.Collections.Generic;

namespace ZeroLevel.Services.Semantic.Model
{
    public class Symbol
    {
        internal static char[] _map_ind_ch = new char[64] { 'а', 'б', 'в', 'г', 'д', 'е', 'ё', 'ж', 'з', 'и', 'й', 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ъ', 'ы', 'ь', 'э', 'ю', 'я', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '-', '.', ',', '!', '?' };

        internal static Dictionary<char, byte> _map_ch_ind = new Dictionary<char, byte>(64)
        {
            {'а', 0}, {'б', 1}, {'в', 2}, {'г', 3}, {'д', 4},
            {'е', 5}, {'ё', 6}, {'ж', 7}, {'з', 8}, {'и', 9},        // 10
            {'й', 10}, {'к', 11}, {'л', 12}, {'м', 13}, {'н', 14},
            {'о', 15}, {'п', 16}, {'р', 17}, {'с', 18}, {'т', 19},   // 20
            {'у', 20}, {'ф', 21}, {'х', 22}, {'ц', 23}, {'ч', 24},
            {'ш', 25}, {'щ', 26}, {'ъ', 27}, {'ы', 28}, {'ь', 29},   // 30
            {'э', 30}, {'ю', 31}, {'я', 32},

            {'a', 33}, {'b', 34}, {'c', 35}, {'d', 36}, {'e', 37},   // 38
            {'f', 38}, {'g', 39}, {'h', 40}, {'i', 41}, {'j', 42},
            {'k', 43}, {'l', 44}, {'m', 45}, {'n', 46}, {'o', 47},   // 48
            {'p', 48}, {'q', 49}, {'r', 50}, {'s', 51}, {'t', 52},
            {'u', 53}, {'v', 54}, {'w', 55}, {'x', 56}, {'y', 57},   // 58
            {'z', 58 },

            { '-', 59}, {'.', 60}, {',', 61}, {'!', 62}, {'?', 63}
        };

        const byte TERMINATE_FLAG = 1;
        const byte HAS_NEXT_FLAG = 2;

        public static byte ToByte(char ch, bool is_leaf = false, bool has_next = false)
        {
            byte b = 0;
            if (_map_ch_ind.TryGetValue(ch, out b))
            {
                b <<= 2;
                if (is_leaf) b |= TERMINATE_FLAG;
                if (has_next) b |= HAS_NEXT_FLAG;
            }
            return b;
        }

        public static bool IsTermiate(byte sym)
        {
            return (sym & TERMINATE_FLAG) == TERMINATE_FLAG;
        }

        public static bool IsLeaf(byte sym)
        {
            return (sym & HAS_NEXT_FLAG) == HAS_NEXT_FLAG;
        }

        public static char ToChar(byte sym)
        {
            var ind = sym >> 2;
            if (ind >= 0 && ind < 64) return _map_ind_ch[ind];
            return '\0';
        }
    }
}
