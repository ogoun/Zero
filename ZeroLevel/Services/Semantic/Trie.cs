using System.Collections.Generic;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Semantic
{
    public class Trie
        : IBinarySerializable
    {
        internal class TrieNode
            : IBinarySerializable
        {
            public char Key;
            public uint? Value;
            public TrieNode Parent;
            public List<TrieNode> Children;

            public TrieNode() { }
            public TrieNode(TrieNode parent) { Parent = parent; }
            public void Deserialize(IBinaryReader reader)
            {
                this.Key = reader.ReadChar();
                if (reader.ReadBoolean())
                {
                    this.Value = reader.ReadUInt32();
                }
                else
                {
                    this.Value = null;
                }
                this.Children = reader.ReadCollection<TrieNode>();
            }

            public void Serialize(IBinaryWriter writer)
            {
                writer.WriteChar(this.Key);
                if (this.Value.HasValue)
                {
                    writer.WriteBoolean(true);
                    writer.WriteUInt32(this.Value.Value);
                }
                else
                {
                    writer.WriteBoolean(false);
                }
                if (this.Children == null)
                {
                    writer.WriteInt32(0);
                }
                else
                {
                    writer.WriteCollection<TrieNode>(this.Children);
                }
            }
            internal TrieNode Append(string word, ref uint word_index, int index, bool reverse)
            {
                if (word.Length == index)
                {
                    if (!this.Value.HasValue)
                    {
                        this.Value = ++word_index;
                        return this;
                    }
                }
                else
                {
                    if (this.Children == null)
                    {
                        this.Children = new List<TrieNode>();
                    }
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (Children[i].Key == word[index])
                        {
                            return Children[i].Append(word, ref word_index, index + 1, reverse);
                        }
                    }
                    var tn = new TrieNode(reverse ? this : null) { Key = word[index] };
                    Children.Add(tn);
                    return tn.Append(word, ref word_index, index + 1, reverse);
                }
                return null;
            }
            internal uint? GetKey(string word, int index)
            {
                if (word.Length == index + 1)
                {
                    return this.Value;
                }
                else
                {
                    if (this.Children == null)
                    {
                        this.Children = new List<TrieNode>();
                    }
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (Children[i].Key == word[index])
                        {
                            return Children[i].GetKey(word, index + 1);
                        }
                    }
                }
                return null;
            }
        }

        internal List<TrieNode> _roots;
        private uint _word_index = 0;
        private readonly bool _use_reverse_index;

        private Dictionary<uint, TrieNode> _reverse_index;
        public Trie(bool reverse_index = false)
        {
            _use_reverse_index = reverse_index;
            if (_use_reverse_index)
            {
                _reverse_index = new Dictionary<uint, TrieNode>();
            }
            _roots = new List<TrieNode>();
        }

        public void Append(string word)
        {
            if (word.Length == 0) return;
            bool found = false;
            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i].Key == word[0])
                {
                    var node = _roots[i].Append(word, ref _word_index, 1, _use_reverse_index);
                    if (_use_reverse_index && node != null)
                    {
                        _reverse_index.Add(node.Value.Value, node);
                    }
                    found = true;
                }
            }
            if (!found)
            {
                var tn = new TrieNode { Key = word[0] };
                _roots.Add(tn);
                var node = tn.Append(word, ref _word_index, 1, _use_reverse_index);
                if (_use_reverse_index && node != null)
                {
                    _reverse_index.Add(node.Value.Value, node);
                }
            }
        }

        public uint? Key(string word)
        {
            if (word?.Length == 0) return null;
            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i].Key == word[0])
                {
                    if (word.Length == 1)
                    {
                        return _roots[i].Value;
                    }
                    else
                    {
                        return _roots[i].GetKey(word, 1);
                    }
                }
            }
            return null;
        }

        public bool Contains(string word)
        {
            if (word?.Length == 0) return false;
            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i].Key == word[0])
                {
                    if (word.Length == 1)
                    {
                        return _roots[i].Value.HasValue;
                    }
                    else
                    {
                        return _roots[i].GetKey(word, 1).HasValue;
                    }
                }
            }
            return false;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteUInt32(this._word_index);
            writer.WriteCollection<TrieNode>(this._roots);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._word_index = reader.ReadUInt32();
            this._roots = reader.ReadCollection<TrieNode>();
        }
    }
}
