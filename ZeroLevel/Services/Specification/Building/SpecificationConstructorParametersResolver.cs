using System;
using System.Collections.Generic;
using ZeroLevel.Services.Trees;

namespace ZeroLevel.Contracts.Specification.Building
{
    public static class SpecificationConstructorParametersResolver
    {
        private class ParamEnum
        {
            public ParamEnum(Dictionary<string, object> map)
            {
                _enum = map;
            }

            private readonly Dictionary<string, object> _enum;

            public IEnumerable<string> Names { get { return _enum.Keys; } }

            public object GetInstance(string name)
            {
                if (_enum.ContainsKey(name))
                    return _enum[name];
                return null!;
            }
        }

        /// <summary>
        /// To select a single value from the list
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, ParamEnum>> _enums =
            new Dictionary<Type, Dictionary<string, ParamEnum>>();

        /// <summary>
        /// To select multiple values from the list
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, ITree>> _trees =
            new Dictionary<Type, Dictionary<string, ITree>>();

        private static readonly object _locker = new object();

        /// <summary>
        /// Registration of enumerable
        /// </summary>
        public static void Register<TFilter>(string paramName, Dictionary<string, object> map)
        {
            if (null == map) throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(paramName)) throw new ArgumentNullException(nameof(paramName));
            var filterType = typeof(TFilter);
            lock (_locker)
            {
                if (false == _enums.ContainsKey(filterType))
                {
                    _enums.Add(filterType, new Dictionary<string, ParamEnum>());
                }
                if (false == _enums[filterType].ContainsKey(paramName))
                {
                    _enums[filterType].Add(paramName, new ParamEnum(map));
                }
            }
        }

        /// <summary>
        /// Tree Registration
        /// </summary>
        public static void RegisterTree<TFilter>(string paramName, ITree tree)
        {
            if (null == tree) throw new ArgumentNullException(nameof(tree));
            if (string.IsNullOrWhiteSpace(paramName)) throw new ArgumentNullException(nameof(paramName));
            var filterType = typeof(TFilter);
            lock (_locker)
            {
                if (false == _trees.ContainsKey(filterType))
                {
                    _trees.Add(filterType, new Dictionary<string, ITree>());
                }
                if (false == _trees[filterType].ContainsKey(paramName))
                {
                    _trees[filterType].Add(paramName, tree);
                }
            }
        }

        public static SpecificationConstructorParameterKind ResolveParameterKind(Type filterType, string paramName)
        {
            if (_enums.ContainsKey(filterType) && _enums[filterType].ContainsKey(paramName))
                return SpecificationConstructorParameterKind.Enum;
            if (_trees.ContainsKey(filterType) && _trees[filterType].ContainsKey(paramName))
                return SpecificationConstructorParameterKind.Tree;
            return SpecificationConstructorParameterKind.None;
        }

        public static IEnumerable<string> GetEnum(Type filterType, string paramName)
        {
            if (_enums.ContainsKey(filterType))
            {
                if (_enums[filterType].ContainsKey(paramName))
                {
                    return _enums[filterType][paramName].Names;
                }
            }
            return null!;
        }

        public static ITree GetTree(Type filterType, string paramName)
        {
            if (_trees.ContainsKey(filterType))
            {
                if (_trees[filterType].ContainsKey(paramName))
                {
                    return _trees[filterType][paramName];
                }
            }
            return null!;
        }

        public static object GetEnumInstance(Type filterType, string paramName, string name)
        {
            if (_enums.ContainsKey(filterType))
            {
                if (_enums[filterType].ContainsKey(paramName))
                {
                    return _enums[filterType][paramName].GetInstance(name);
                }
            }
            return null!;
        }
    }
}