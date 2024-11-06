using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Semantic.Model;

///
/// https://github.com/kpol/trie
///

namespace ZeroLevel.Services.Semantic.Search
{
    public sealed class PrefixTrie 
        : ICollection<string>, IReadOnlyCollection<string>
    {
        private readonly IEqualityComparer<char> _comparer;

        private readonly CharTrieNode _root = new(char.MinValue);

        public PrefixTrie(IEqualityComparer<char>? comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<char>.Default;
        }

        public int Count { get; private set; }

        bool ICollection<string>.IsReadOnly => false;

        public bool Add(string word)
        {
            if(string.IsNullOrWhiteSpace(word)) throw new ArgumentException(nameof(word));

            var (existingTerminalNode, parent) = AddNodesFromUpToBottom(word);

            if (existingTerminalNode is not null && existingTerminalNode.IsTerminal) return false; // already exists

            var newTerminalNode = new TerminalCharTrieNode(word[^1]) { Word = word };

            AddTerminalNode(parent, existingTerminalNode, newTerminalNode, word);

            return true;
        }

        public void Clear()
        {
            _root.Children = [];
            Count = 0;
        }

        public bool Contains(string word) => Contains(word.AsSpan());

        public int IntersectionWith(string word) => IntersectionWith(word.AsSpan());

        public int IntersectionWith(ReadOnlySpan<char> word)
        {
            if (word.IsEmpty)
            {
                return 0;
            }
            return CalculateIntersection(word);
        }

        public bool Contains(ReadOnlySpan<char> word)
        {
            if (word.IsEmpty)
            {
                if (string.IsNullOrWhiteSpace(word.ToString())) throw new ArgumentException(nameof(word));
            }

            var node = GetNode(word);

            return node is not null && node.IsTerminal;
        }

        public bool Remove(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException(nameof(word));

            var nodesUpToBottom = GetNodesForRemoval(word);

            if (nodesUpToBottom.Count == 0) return false;

            RemoveNode(nodesUpToBottom);

            return true;
        }

        public IEnumerable<string> StartsWith(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(nameof(value));

            return _();

            IEnumerable<string> _() => GetTerminalNodesByPrefix(value).Select(n => n.Word);
        }

        public IEnumerable<string> Matches(IReadOnlyList<Character> pattern)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if(pattern.Count == 0) throw new ArgumentOutOfRangeException(nameof(pattern));

            return _();

            IEnumerable<string> _() =>
                GetNodesByPattern(pattern)
                    .Where(n => n.IsTerminal)
                    .Cast<TerminalCharTrieNode>()
                    .Select(n => n.Word);
        }

        public IEnumerable<string> StartsWith(IReadOnlyList<Character> pattern)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (pattern.Count == 0) throw new ArgumentOutOfRangeException(nameof(pattern));

            return _();

            IEnumerable<string> _()
            {
                foreach (var n in GetNodesByPattern(pattern))
                {
                    if (n.IsTerminal)
                    {
                        yield return ((TerminalCharTrieNode)n).Word;
                    }

                    foreach (var terminalNode in GetDescendantTerminalNodes(n))
                    {
                        yield return terminalNode.Word;
                    }
                }
            }
        }

        internal (CharTrieNode? existingTerminalNode, CharTrieNode parent) AddNodesFromUpToBottom(ReadOnlySpan<char> word)
        {
            var current = _root;

            for (int i = 0; i < word.Length - 1; i++)
            {
                var n = GetChildNode(current, word[i]);

                if (n is not null)
                {
                    current = n;
                }
                else
                {
                    CharTrieNode node = new(word[i]);
                    AddToNode(current, node);
                    current = node;
                }
            }

            var terminalNode = GetChildNode(current, word[^1]);

            return (terminalNode, current);
        }

        internal void AddTerminalNode(CharTrieNode parent, CharTrieNode? existingNode, CharTrieNode newTerminalNode, string word)
        {
            if (existingNode is not null)
            {
                newTerminalNode.CopyChildren(existingNode.Children);

                RemoveChildFromNode(parent, word[^1]);
            }

            AddToNode(parent, newTerminalNode);
            Count++;
        }

        internal IEnumerable<TerminalCharTrieNode> GetTerminalNodesByPrefix(ReadOnlySpan<char> prefix)
        {
            var node = GetNode(prefix);
            return GetTerminalNodes(node);
        }

        private IEnumerable<TerminalCharTrieNode> GetTerminalNodes(CharTrieNode? node)
        {
            if (node is null)
            {
                yield break;
            }

            if (node.IsTerminal)
            {
                yield return (TerminalCharTrieNode)node;
            }

            foreach (var n in GetDescendantTerminalNodes(node))
            {
                yield return n;
            }
        }

        public IEnumerator<string> GetEnumerator() => GetAllTerminalNodes().Select(n => n.Word).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<string>.Add(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException(nameof(word));

            Add(word);
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex)
        {
            if(array == null) throw new ArgumentNullException(nameof(array));
            if(arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (Count > array.Length - arrayIndex)
            {
                throw new ArgumentException(
                    "The number of elements in the trie is greater than the available space from index to the end of the destination array.");
            }

            foreach (var node in GetAllTerminalNodes())
            {
                array[arrayIndex++] = node.Word;
            }
        }

        internal IEnumerable<TerminalCharTrieNode> GetAllTerminalNodes() => GetDescendantTerminalNodes(_root);

        internal static IEnumerable<TerminalCharTrieNode> GetDescendantTerminalNodes(CharTrieNode node)
        {
            Queue<CharTrieNode> queue = new(node.Children);

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();

                if (n.IsTerminal)
                {
                    yield return (TerminalCharTrieNode)n;
                }

                for (var i = 0; i < n.Children.Length; i++)
                {
                    queue.Enqueue(n.Children[i]);
                }
            }
        }

        internal int CalculateIntersection(ReadOnlySpan<char> prefix)
        {
            var current = _root;

            for (var i = 0; i < prefix.Length; i++)
            {
                current = GetChildNode(current, prefix[i]);

                if (current is null)
                {
                    return i;
                }
            }

            return prefix.Length;
        }

        internal CharTrieNode? GetNode(ReadOnlySpan<char> prefix)
        {
            var current = _root;

            for (var i = 0; i < prefix.Length; i++)
            {
                current = GetChildNode(current, prefix[i]);

                if (current is null)
                {
                    return null;
                }
            }

            return current;
        }

        internal IEnumerable<CharTrieNode> GetNodesByPattern(IReadOnlyList<Character> pattern)
        {
            Queue<(CharTrieNode node, int index)> queue = [];
            queue.Enqueue((_root, 0));

            while (queue.Count > 0)
            {
                var (node, index) = queue.Dequeue();

                if (index == pattern.Count - 1)
                {
                    if (pattern[index] != Character.Any)
                    {
                        var n = GetChildNode(node, pattern[index].Char);

                        if (n is not null)
                        {
                            yield return n;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < node.Children.Length; i++)
                        {
                            yield return node.Children[i];
                        }
                    }
                }
                else
                {
                    if (pattern[index] != Character.Any)
                    {
                        var n = GetChildNode(node, pattern[index].Char);

                        if (n is not null)
                        {
                            queue.Enqueue((n, index + 1));
                        }
                    }
                    else
                    {
                        for (var i = 0; i < node.Children.Length; i++)
                        {
                            queue.Enqueue((node.Children[i], index + 1));
                        }
                    }
                }
            }
        }

        private Stack<CharTrieNode> GetNodesForRemoval(string prefix)
        {
            var current = _root;

            Stack<CharTrieNode> nodesUpToBottom = [];
            nodesUpToBottom.Push(_root);

            for (var i = 0; i < prefix.Length; i++)
            {
                var c = prefix[i];
                current = GetChildNode(current, c);

                if (current is not null)
                {
                    nodesUpToBottom.Push(current);
                }
                else
                {
                    return [];
                }
            }

            return current.IsTerminal ? nodesUpToBottom : [];
        }

        private void RemoveNode(Stack<CharTrieNode> nodesUpToBottom)
        {
            Count--;

            var node = nodesUpToBottom.Pop();

            if (node.Children.Length == 0)
            {
                while (node.Children.Length == 0 && nodesUpToBottom.Count > 0)
                {
                    var parent = nodesUpToBottom.Pop();
                    RemoveChildFromNode(parent, node.Key);

                    if (parent.IsTerminal) return;

                    node = parent;

                }
            }
            else
            {
                // convert node to non-terminal node
                CharTrieNode n = new(node.Key);
                n.CopyChildren(node.Children);

                var parent = nodesUpToBottom.Count == 0 ? _root : nodesUpToBottom.Pop();

                RemoveChildFromNode(parent, node.Key);
                AddToNode(parent, n);
            }
        }

        private void AddToNode(CharTrieNode node, CharTrieNode nodeToAdd)
        {
            for (var i = 0; i < node.Children.Length; i++)
            {
                if (_comparer.Equals(nodeToAdd.Key, node.Children[i].Key))
                {
                    return;
                }
            }

            node.AddChild(nodeToAdd);
        }

        private void RemoveChildFromNode(CharTrieNode node, char key)
        {
            for (int i = 0; i < node.Children.Length; i++)
            {
                if (_comparer.Equals(key, node.Children[i].Key))
                {
                    node.RemoveChildAt(i);

                    break;
                }
            }
        }

        private CharTrieNode? GetChildNode(CharTrieNode node, char key)
        {
            for (var i = 0; i < node.Children.Length; i++)
            {
                var n = node.Children[i];

                if (_comparer.Equals(key, n.Key))
                {
                    return n;
                }
            }

            return null;
        }
    }
}
