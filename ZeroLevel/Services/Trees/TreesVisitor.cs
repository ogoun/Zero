using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Trees
{
    public static class TreesVisitor
    {
        /// <summary>
        /// Extract tree branches to plain array
        /// </summary>
        /// <typeparam name="T">Node type</typeparam>
        /// <param name="root">Tree root</param>
        /// <param name="childrenExtractor">Extractor of node children</param>
        /// <returns>Array of tree branches</returns>
        public static List<T[]> ExtractBranches<T>(T root, Func<T, IEnumerable<T>> childrenExtractor)
        {
            if (root == null!)
                throw new ArgumentNullException(nameof(root));
            if (childrenExtractor == null!)
                throw new ArgumentNullException(nameof(childrenExtractor));
            var result = new List<T[]>();
            TraversTreeBrunches(root, childrenExtractor, brunch =>
            {
                result.Add(brunch.ToArray());
            });
            return result;
        }

        /// <summary>
        /// Selects all branches of the tree, returning an array of branches consisting of the specified node values.
        /// </summary>
        /// <typeparam name="T">Type of tree nodes</typeparam>
        /// <typeparam name="TCode">Value type for returned branch elements</typeparam>
        /// <param name="root">Root</param>
        /// <param name="childrenExtractor">Selects child nodes for the current node.</param>
        /// <param name="codeExtractor">Select the value of the node</param>
        /// <returns>List of tree branches</returns>
        public static List<TCode[]> SpecifyExtractBranches<T, TCode>(T root,
            Func<T, IEnumerable<T>> childrenExtractor,
            Func<T, TCode> codeExtractor)
        {
            if (root == null!)
                throw new ArgumentNullException(nameof(root));
            if (childrenExtractor == null!)
                throw new ArgumentNullException(nameof(childrenExtractor));
            if (codeExtractor == null!)
                throw new ArgumentNullException(nameof(codeExtractor));
            var result = new List<TCode[]>();
            TraversTreeBrunches(root, childrenExtractor, brunch =>
            {
                result.Add(brunch.Select(i => codeExtractor(i)).ToArray());
            });
            return result;
        }

        /// <summary>
        /// Performs tree branch traversal.
        /// </summary>
        public static void TraversTreeBrunches<T>(T root,
            Func<T, IEnumerable<T>> childrenExtractor,
            Action<IEnumerable<T>> handler)
        {
            if (root == null!)
                throw new ArgumentNullException(nameof(root));
            if (childrenExtractor == null!)
                throw new ArgumentNullException(nameof(childrenExtractor));
            if (handler == null!)
                throw new ArgumentNullException(nameof(handler));
            var brunch = new List<T>();
            brunch.Add(root);
            foreach (var child in childrenExtractor(root))
            {
                TraversNode<T>(child, brunch, childrenExtractor, handler);
            }
        }

        private static void TraversNode<T>(T node, List<T> brunch, Func<T, IEnumerable<T>> childrenExtractor, Action<IEnumerable<T>> handler)
        {
            if (node == null!)
            {
                handler(brunch);
                return;
            }
            var currentBrunch = new List<T>(brunch);
            currentBrunch.Add(node);
            var children = childrenExtractor(node);
            if (children != null && children.Any())
            {
                foreach (var child in childrenExtractor(node))
                {
                    TraversNode<T>(child, currentBrunch, childrenExtractor, handler);
                }
            }
            else
            {
                handler(currentBrunch);
            }
        }
    }
}