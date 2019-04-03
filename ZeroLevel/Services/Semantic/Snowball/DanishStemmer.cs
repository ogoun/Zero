/*
 *  Port of Snowball stemmers on C#
 *  Original stemmers can be found on http://snowball.tartarus.org
 *  Licence still BSD: http://snowball.tartarus.org/license.php
 *
 *  Most of stemmers are ported from Java by Iveonik Systems ltd. (www.iveonik.com)
 */

using System.Text;
using ZeroLevel.Services.Semantic;

namespace Iveonik.Stemmers
{
    public class DanishStemmer : StemmerOperations, ILexer
    {
        private readonly static DanishStemmer methodObject = new DanishStemmer();

        private readonly static Among[] a_0 =
        {
            new Among ( "hed", -1, 1, null ),
            new Among ( "ethed", 0, 1, null ),
            new Among ( "ered", -1, 1, null ),
            new Among ( "e", -1, 1, null ),
            new Among ( "erede", 3, 1, null ),
            new Among ( "ende", 3, 1, null ),
            new Among ( "erende", 5, 1, null ),
            new Among ( "ene", 3, 1, null ),
            new Among ( "erne", 3, 1, null ),
            new Among ( "ere", 3, 1, null ),
            new Among ( "en", -1, 1, null ),
            new Among ( "heden", 10, 1, null ),
            new Among ( "eren", 10, 1, null ),
            new Among ( "er", -1, 1, null ),
            new Among ( "heder", 13, 1, null ),
            new Among ( "erer", 13, 1, null ),
            new Among ( "s", -1, 2, null ),
            new Among ( "heds", 16, 1, null ),
            new Among ( "es", 16, 1, null ),
            new Among ( "endes", 18, 1, null ),
            new Among ( "erendes", 19, 1, null ),
            new Among ( "enes", 18, 1, null ),
            new Among ( "ernes", 18, 1, null ),
            new Among ( "eres", 18, 1, null ),
            new Among ( "ens", 16, 1, null ),
            new Among ( "hedens", 24, 1, null ),
            new Among ( "erens", 24, 1, null ),
            new Among ( "ers", 16, 1, null ),
            new Among ( "ets", 16, 1, null ),
            new Among ( "erets", 28, 1, null ),
            new Among ( "et", -1, 1, null ),
            new Among ( "eret", 30, 1, null )
        };

        private readonly static Among[] a_1 =
        {
            new Among ( "gd", -1, -1, null ),
            new Among ( "dt", -1, -1, null ),
            new Among ( "gt", -1, -1, null ),
            new Among ( "kt", -1, -1, null )
        };

        private readonly static Among[] a_2 =
        {
            new Among ( "ig", -1, 1, null ),
            new Among ( "lig", 0, 1, null ),
            new Among ( "elig", 1, 1, null ),
            new Among ( "els", -1, 1, null ),
            new Among ( "l\u00F8st", -1, 2, null )
        };

        private static readonly char[] g_v = {(char)17, (char)65, (char)16, (char)1, (char)0, (char)0, (char)0,
                                                 (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
                                                 (char)0, (char)0, (char)48,(char)0, (char)128 };

        private static readonly char[] g_s_ending = { (char)239, (char)254, (char)42, (char)3, (char)0, (char)0,
                                                        (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
                                                        (char)0, (char)0, (char)0, (char)0, (char)16 };

        private int I_x;
        private int I_p1;
        private StringBuilder S_ch = new StringBuilder();

        private void copy_from(DanishStemmer other)
        {
            I_x = other.I_x;
            I_p1 = other.I_p1;
            S_ch = other.S_ch;
            base.copy_from(other);
        }

        private bool r_mark_regions()
        {
            bool subroot = false;
            int v_1;
            int v_2;
            // (, line 29
            I_p1 = limit;
            // test, line 33
            v_1 = cursor;
            // (, line 33
            // hop, line 33
            {
                int c = cursor + 3;
                if (0 > c || c > limit)
                {
                    return false;
                }
                cursor = c;
            }
            // setmark x, line 33
            I_x = cursor;
            cursor = v_1;
            // goto, line 34
            while (true)
            {
                v_2 = cursor;
                do
                {
                    if (!(in_grouping(g_v, 97, 248)))
                    {
                        break;
                    }
                    cursor = v_2;
                    subroot = true;
                    if (subroot) break;
                } while (false);
                if (subroot) { subroot = false; break; }
                cursor = v_2;
                if (cursor >= limit)
                {
                    return false;
                }
                cursor++;
            }
            // gopast, line 34
            while (true)
            {
                do
                {
                    if (!(out_grouping(g_v, 97, 248)))
                    {
                        break;
                    }
                    subroot = true;
                    if (subroot) break;
                } while (false);
                if (subroot) { subroot = false; break; }
                if (cursor >= limit)
                {
                    return false;
                }
                cursor++;
            }
            // setmark p1, line 34
            I_p1 = cursor;
            // try, line 35
            do
            {
                // (, line 35
                if (!(I_p1 < I_x))
                {
                    break;
                }
                I_p1 = I_x;
            } while (false);
            return true;
        }

        private bool r_main_suffix()
        {
            int among_var;
            int v_1;
            int v_2;
            // (, line 40
            // setlimit, line 41
            v_1 = limit - cursor;
            // tomark, line 41
            if (cursor < I_p1)
            {
                return false;
            }
            cursor = I_p1;
            v_2 = limit_backward;
            limit_backward = cursor;
            cursor = limit - v_1;
            // (, line 41
            // [, line 41
            ket = cursor;
            // substring, line 41
            among_var = find_among_b(a_0, 32);
            if (among_var == 0)
            {
                limit_backward = v_2;
                return false;
            }
            // ], line 41
            bra = cursor;
            limit_backward = v_2;
            switch (among_var)
            {
                case 0:
                    return false;

                case 1:
                    // (, line 48
                    // delete, line 48
                    slice_del();
                    break;

                case 2:
                    // (, line 50
                    if (!(in_grouping_b(g_s_ending, 97, 229)))
                    {
                        return false;
                    }
                    // delete, line 50
                    slice_del();
                    break;
            }
            return true;
        }

        private bool r_consonant_pair()
        {
            int v_1;
            int v_2;
            int v_3;
            // (, line 54
            // test, line 55
            v_1 = limit - cursor;
            // (, line 55
            // setlimit, line 56
            v_2 = limit - cursor;
            // tomark, line 56
            if (cursor < I_p1)
            {
                return false;
            }
            cursor = I_p1;
            v_3 = limit_backward;
            limit_backward = cursor;
            cursor = limit - v_2;
            // (, line 56
            // [, line 56
            ket = cursor;
            // substring, line 56
            if (find_among_b(a_1, 4) == 0)
            {
                limit_backward = v_3;
                return false;
            }
            // ], line 56
            bra = cursor;
            limit_backward = v_3;
            cursor = limit - v_1;
            // next, line 62
            if (cursor <= limit_backward)
            {
                return false;
            }
            cursor--;
            // ], line 62
            bra = cursor;
            // delete, line 62
            slice_del();
            return true;
        }

        private bool r_other_suffix()
        {
            int among_var;
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            // (, line 65
            // do, line 66
            v_1 = limit - cursor;
            do
            {
                // (, line 66
                // [, line 66
                ket = cursor;
                // literal, line 66
                if (!(eq_s_b(2, "st")))
                {
                    break;
                }
                // ], line 66
                bra = cursor;
                // literal, line 66
                if (!(eq_s_b(2, "ig")))
                {
                    break;
                }
                // delete, line 66
                slice_del();
            } while (false);
            cursor = limit - v_1;
            // setlimit, line 67
            v_2 = limit - cursor;
            // tomark, line 67
            if (cursor < I_p1)
            {
                return false;
            }
            cursor = I_p1;
            v_3 = limit_backward;
            limit_backward = cursor;
            cursor = limit - v_2;
            // (, line 67
            // [, line 67
            ket = cursor;
            // substring, line 67
            among_var = find_among_b(a_2, 5);
            if (among_var == 0)
            {
                limit_backward = v_3;
                return false;
            }
            // ], line 67
            bra = cursor;
            limit_backward = v_3;
            switch (among_var)
            {
                case 0:
                    return false;

                case 1:
                    // (, line 70
                    // delete, line 70
                    slice_del();
                    // do, line 70
                    v_4 = limit - cursor;
                    do
                    {
                        // call consonant_pair, line 70
                        if (!r_consonant_pair())
                        {
                            break;
                        }
                    } while (false);
                    cursor = limit - v_4;
                    break;

                case 2:
                    // (, line 72
                    // <-, line 72
                    slice_from("l\u00F8s");
                    break;
            }
            return true;
        }

        private bool r_undouble()
        {
            int v_1;
            int v_2;
            // (, line 75
            // setlimit, line 76
            v_1 = limit - cursor;
            // tomark, line 76
            if (cursor < I_p1)
            {
                return false;
            }
            cursor = I_p1;
            v_2 = limit_backward;
            limit_backward = cursor;
            cursor = limit - v_1;
            // (, line 76
            // [, line 76
            ket = cursor;
            if (!(out_grouping_b(g_v, 97, 248)))
            {
                limit_backward = v_2;
                return false;
            }
            // ], line 76
            bra = cursor;
            // -> ch, line 76
            S_ch = slice_to(S_ch);
            limit_backward = v_2;
            // name ch, line 77
            if (!(eq_v_b(S_ch)))
            {
                return false;
            }
            // delete, line 78
            slice_del();
            return true;
        }

        private bool CanStem()
        {
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            int v_5;
            // (, line 82
            // do, line 84
            v_1 = cursor;
            do
            {
                // call mark_regions, line 84
                if (!r_mark_regions())
                {
                    break;
                }
            } while (false);
            cursor = v_1;
            // backwards, line 85
            limit_backward = cursor; cursor = limit;
            // (, line 85
            // do, line 86
            v_2 = limit - cursor;
            do
            {
                // call main_suffix, line 86
                if (!r_main_suffix())
                {
                    break;
                }
            } while (false);
            cursor = limit - v_2;
            // do, line 87
            v_3 = limit - cursor;
            do
            {
                // call consonant_pair, line 87
                if (!r_consonant_pair())
                {
                    break;
                }
            } while (false);
            cursor = limit - v_3;
            // do, line 88
            v_4 = limit - cursor;
            do
            {
                // call other_suffix, line 88
                if (!r_other_suffix())
                {
                    break;
                }
            } while (false);
            cursor = limit - v_4;
            // do, line 89
            v_5 = limit - cursor;
            do
            {
                // call undouble, line 89
                if (!r_undouble())
                {
                    break;
                }
            } while (false);
            cursor = limit - v_5;
            cursor = limit_backward;
            return true;
        }

        public string Lex(string s)
        {
            this.setCurrent(s.ToLowerInvariant());
            this.CanStem();
            return this.getCurrent();
        }
    }
}