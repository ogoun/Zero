using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Text
{
    internal struct state
    {
        public int len;
        public int link;
        public Dictionary<char, int> next;
    }

    public class SuffixAutomata
    {
        const int MAXLEN = 100000;
        private state[] st = new state[MAXLEN * 2];
        int sz, last;

        public void Init(bool is_reused = false)
        {
            sz = last = 0;
            st[0].len = 0;
            st[0].link = -1;
            ++sz;
            // этот код нужен, только если автомат строится много раз для разных строк:
            for (int i = 0; i < MAXLEN * 2; ++i)
            {
                st[i].next = new Dictionary<char, int>();
            }
        }

        public void Extend(char c)
        {
            int cur = sz++;
            st[cur].len = st[last].len + 1;
            int p;
            for (p = last; p != -1 && !st[p].next.ContainsKey(c); p = st[p].link)
                st[p].next[c] = cur;
            if (p == -1)
                st[cur].link = 0;
            else
            {
                int q = st[p].next[c];
                if (st[p].len + 1 == st[q].len)
                    st[cur].link = q;
                else
                {
                    int clone = sz++;
                    st[clone].len = st[p].len + 1;
                    st[clone].next = st[q].next;
                    st[clone].link = st[q].link;
                    for (; p != -1 && st[p].next.ContainsKey(c) && st[p].next[c] == q; p = st[p].link)
                        st[p].next[c] = clone;
                    st[q].link = st[cur].link = clone;
                }
            }
            last = cur;
        }

        public bool IsSubstring(string w)
        {
            if (string.IsNullOrEmpty(w)) return true;
            bool fail = false;
            int n, si = 0;
            for (; si < last; si++)
            {
                if (st[si].next.ContainsKey(w[0]))
                {
                    var start = st[si];
                    for (int i = 0; i < w.Length; i++)
                    {
                        if (start.next.ContainsKey(w[i]) == false)
                        {
                            fail = true;
                            break;
                        }
                        n = start.next[w[i]];
                        start = st[n];
                    }
                    break;
                }
            }
            if (si == last)
            {
                fail = true;
            }
            return (!fail);
        }

        public string Intersection(string t)
        {
            var entries = st.Where(x => x.next.ContainsKey(t[0])).ToArray();
            var candidates = entries
                .Select(s => Intersection(s, t));
            if (candidates != null && candidates.Any())
            {
                var max = candidates.Max(s => s?.Length ?? 0);
                return candidates.FirstOrDefault(c => c != null && c.Length == max);
            }
            return null!;
            /*
            int v = 0, l = 0, best = 0, bestpos = 0;
            for (int i = 0; i < (int)t.Length; ++i)
            {
                while (v > 0 && !st[v].next.ContainsKey(t[i]))
                {
                    v = st[v].link;
                    l = st[v].len;
                }
                if (st[v].next.ContainsKey(t[i]))
                {
                    v = st[v].next[t[i]];
                    ++l;
                }
                if (l > best)
                {
                    best = l;
                    bestpos = i;
                }
            }
            var start = bestpos - best + 1;
            var length = best;
            if (start >= 0 && start < t.Length && (start + length) <= t.Length)
                return t.Substring(start, length);
            return null!;
            */
        }

        private string Intersection(state entry, string t)
        {
            int v = 0, l = 0, best = 0, bestpos = 0;
            for (int i = 0; i < (int)t.Length; ++i)
            {
                while (v > 0 && !entry.next.ContainsKey(t[i]))
                {
                    v = entry.link;
                    l = entry.len;
                    entry = st[v];
                }
                if (entry.next.ContainsKey(t[i]))
                {
                    v = entry.next[t[i]];
                    entry = st[v];
                    ++l;
                }
                if (l > best)
                {
                    best = l;
                    bestpos = i;
                }
            }
            var start = bestpos - best + 1;
            var length = best;
            if (start >= 0 && start < t.Length && (start + length) <= t.Length)
                return t.Substring(start, length);
            return null!;
        }
    }
}
