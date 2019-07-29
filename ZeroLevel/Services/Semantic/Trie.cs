using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Semantic
{
    public class Trie
        : IBinarySerializable
    {
        private class TrieNode
            : IBinarySerializable
        {
            public char Key;
            public uint? Value;
            public List<TrieNode> Children;

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

            internal void Append(string word, ref uint word_index, int index)
            {
                if (word.Length == index + 1)
                {
                    if (!this.Value.HasValue)
                    {
                        this.Value = ++word_index;
                    }
                }
                else
                {
                    if (this.Children == null)
                    {
                        this.Children = new List<TrieNode>();
                    }
                    bool found = false;
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (Children[i].Key == word[index])
                        {
                            Children[i].Append(word, ref word_index, index + 1);
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        var tn = new TrieNode { Key = word[index] };
                        Children.Add(tn);
                        tn.Append(word, ref word_index, index + 1);
                    }
                }
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

        private List<TrieNode> _roots;
        private uint _word_index = 0;

        public Trie()
        {
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
                    _roots[i].Append(word, ref _word_index, 1);
                    found = true;
                }
            }
            if (!found)
            {
                var tn = new TrieNode { Key = word[0] };
                _roots.Add(tn);
                tn.Append(word, ref _word_index, 1);
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
