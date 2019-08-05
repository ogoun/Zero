using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            public char? Key; // setted only with rebuild index
            public uint? Value;
            public TrieNode Parent;
            public ConcurrentDictionary<char, TrieNode> Children;

            public TrieNode() { }
            public TrieNode(TrieNode parent) { Parent = parent; }
            public void Deserialize(IBinaryReader reader)
            {
                if (reader.ReadBoolean())
                {
                    this.Value = reader.ReadUInt32();
                }
                else
                {
                    this.Value = null;
                }
                this.Children = reader.ReadDictionaryAsConcurrent<char, TrieNode>();
            }

            public void Serialize(IBinaryWriter writer)
            {
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
                    writer.WriteDictionary<char, TrieNode>(this.Children);
                }
            }
            internal TrieNode Append(string word, int index, bool reverse)
            {
                if (word.Length == index)
                {
                    if (!this.Value.HasValue)
                    {
                        return this;
                    }
                }
                else
                {
                    if (this.Children == null)
                    {
                        this.Children = new ConcurrentDictionary<char, TrieNode>();
                    }
                    if (!this.Children.ContainsKey(word[index]))
                    {
                        this.Children.TryAdd(word[index], new TrieNode(this));
                    }
                    return Children[word[index]].Append(word, index + 1, reverse);
                }
                return null;
            }
            internal uint? GetKey(string word, int index)
            {
                if (this.Children?.ContainsKey(word[index]) ?? false)
                {
                    if (word.Length == index + 1) return this.Children[word[index]].Value;
                    return this.Children[word[index]].GetKey(word, index + 1);
                }
                return null;
            }

            internal void RebuildReverseIndex(TrieNode parent, char key, Dictionary<uint, TrieNode> index)
            {
                this.Key = key;
                this.Parent = parent;
                if (this.Value.HasValue)
                {
                    index.Add(this.Value.Value, this);
                }
                if (this.Children != null)
                {
                    foreach (var child in this.Children)
                    {
                        child.Value.RebuildReverseIndex(this, child.Key, index);
                    }
                }
            }

            internal void DestroyReverseIndex()
            {
                this.Parent = null;
                this.Key = null;
                if (this.Children != null)
                {
                    foreach (var child in this.Children)
                    {
                        child.Value.DestroyReverseIndex();
                    }
                }
            }
        }

        internal TrieNode _root;
        private int _word_index = 0;
        private bool _use_reverse_index;

        private Dictionary<uint, TrieNode> _reverse_index;

        public IEnumerable<uint> Keys => _reverse_index.Keys;

        public Trie() : this(false)
        {
        }

        public Trie(bool reverse_index = false)
        {
            _use_reverse_index = reverse_index;
            if (_use_reverse_index)
            {
                _reverse_index = new Dictionary<uint, TrieNode>();
            }
            _root = new TrieNode();
        }

        public void ToggleReverseIndex(bool enabled)
        {
            if (_use_reverse_index == enabled) return;
            _use_reverse_index = enabled;
            if (_use_reverse_index)
            {
                RebuildReverseIndex();
            }
            else
            {
                DestroyReverseIndex();
            }
        }
        public void Append(string word)
        {
            if (word.Length == 0) return;
            var node = _root.Append(word, 0, _use_reverse_index);
            if (node != null)
            {
                node.Value = (uint)Interlocked.Increment(ref _word_index);
                if (_use_reverse_index)
                {
                    _reverse_index.Add(node.Value.Value, node);
                }
            }
        }

        public uint? Key(string word)
        {
            if (word?.Length == 0) return null;
            return _root.GetKey(word, 0);
        }

        public string Word(uint key)
        {
            if (_use_reverse_index)
            {
                if (_reverse_index.ContainsKey(key))
                {
                    var node = _reverse_index[key];
                    return new string(Backward(node).Reverse().ToArray());
                }
            }
            return null;
        }

        private IEnumerable<char> Backward(TrieNode node)
        {
            if (_use_reverse_index)
            {
                do
                {
                    yield return node.Key.Value;
                    node = node.Parent;
                } while (node.Parent != null);
            }
        }

        public bool Contains(string word)
        {
            if (word?.Length == 0) return false;
            return _root.GetKey(word, 0).HasValue;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this._word_index);
            writer.WriteBoolean(this._use_reverse_index);
            writer.Write<TrieNode>(this._root);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._word_index = reader.ReadInt32();
            this._use_reverse_index = reader.ReadBoolean();
            this._root = reader.Read<TrieNode>();
            RebuildReverseIndex();
        }

        private void RebuildReverseIndex()
        {
            if (this._use_reverse_index)
            {
                if (_reverse_index == null)
                {
                    _reverse_index = new Dictionary<uint, TrieNode>();
                }
                _root.RebuildReverseIndex(null, ' ', _reverse_index);
            }
        }

        private void DestroyReverseIndex()
        {
            if (_reverse_index != null)
            {
                _reverse_index.Clear();
                _reverse_index = null;
            }
            _root.DestroyReverseIndex();
        }
    }
}
