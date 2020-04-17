using System.Collections.Generic;
using System.Text;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Trees
{
    public class State
        : IBinarySerializable
    {
        private bool _is_teminate;
        private Dictionary<char, State> _transtions;

        public State()
        {
            _is_teminate = false;
            _transtions = new Dictionary<char, State>(32);
        }

        public bool Append(string word, int position)
        {
            if (word.Length == position)
            {
                if (_is_teminate)
                {
                    return false;
                }
                _is_teminate = true;
                return true;
            }
            State next;
            if (_transtions.TryGetValue(word[position], out next) == false)
            {
                next = new State();
                _transtions[word[position]] = next;
            }
            return next.Append(word, position + 1);
        }

        public bool Contains(string w, int position)
        {
            if (w.Length == position) return _is_teminate;
            State next;
            if (_transtions.TryGetValue(w[position], out next))
            {
                return next.Contains(w, position + 1);
            }
            return false;
        }

        public IEnumerable<string> Iterator(StringBuilder sb)
        {
            if (_is_teminate)
            {
                yield return sb.ToString();
            }
            foreach (var s in _transtions)
            {
                sb.Append(s.Key);
                foreach (var t in s.Value.Iterator(sb))
                {
                    yield return t;
                }
                sb.Remove(sb.Length - 1, 1);
            }
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(this._is_teminate);
            writer.WriteDictionary(this._transtions);
        }

        public void Deserialize(IBinaryReader reader)
        {
            _transtions.Clear();
            this._is_teminate = reader.ReadBoolean();
            this._transtions = reader.ReadDictionary<char, State>();
        }
    }

    public class DSA
        : IBinarySerializable
    {
        private State _initialState;
        private long _count = 0;

        public long Count => _count;

        public DSA()
        {
            _initialState = new State();
        }

        public bool AppendWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            if (_initialState.Append(word, 0))
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            return false;
        }

        public bool Contains(string word)
        {
            return _initialState.Contains(word, 0);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._count = reader.ReadLong();
            _initialState.Deserialize(reader);
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this._count);
            _initialState.Serialize(writer);
        }

        public IEnumerable<string> Iterator()
        {
            return _initialState.Iterator(new StringBuilder());
        }
    }
}
