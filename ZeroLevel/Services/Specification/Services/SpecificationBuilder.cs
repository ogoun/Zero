using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Contracts.Specification.Building;
using ZeroLevel.Services.Trees;

namespace ZeroLevel.Specification
{
    /// <summary>
    /// Creates a specification using a specific constructor.
    /// </summary>
    public class SpecificationBuilder : 
        ISpecificationBuilder, 
        IEquatable<SpecificationBuilder>
    {
        /// <summary>
        /// Type of specification
        /// </summary>
        private readonly Type _instanceType;
        /// <summary>
        /// List of Constructor Parameters
        /// </summary>
        private readonly List<SpecificationParameter> _values =
            new List<SpecificationParameter>();
        /// <summary>
        /// Constructor name
        /// </summary>
        public string Name { get; }
        public Type FilterType { get { return _instanceType; } }
        public IEnumerable<SpecificationParameter> Parameters { get { return _values; } }

        internal SpecificationBuilder(string name,
            List<SpecificationParameter> parameters,
            Type specificationType)
        {
            Name = name;
            _instanceType = specificationType;
            _values = parameters;
        }
        /// <summary>
        /// Parameter traversal
        /// </summary>
        public void ParametersTraversal(Action<SpecificationParameter> parameterHandler)
        {
            foreach (var p in _values)
            {
                parameterHandler(p);
            }
        }
        /// <summary>
        /// Build specification
        /// </summary>
        public ISpecification<T> Build<T>()
        {
            var parameters = new object[_values.Count];
            for (int i = 0; i < _values.Count; i++)
            {
                switch (SpecificationConstructorParametersResolver.
                    ResolveParameterKind(_instanceType, _values[i].ParameterName))
                {
                    case SpecificationConstructorParameterKind.Enum:
                        if (_values[i].Value is string[])
                        {
                            parameters[i] = (_values[i].Value as string[]).
                                Select(name =>
                                SpecificationConstructorParametersResolver.
                                GetEnumInstance(_instanceType, _values[i].ParameterName, name)).
                                ToArray();
                        }
                        else
                        {
                            parameters[i] = SpecificationConstructorParametersResolver.GetEnumInstance(_instanceType, _values[i].ParameterName, _values[i].Value as string);
                        }
                        break;
                    case SpecificationConstructorParameterKind.Tree:
                        var tree = (ITree)_values[i].Value;
                        var list = new List<object>();
                        Action<ITreeNode> visitor = null;
                        visitor = new Action<ITreeNode>(node =>
                        {
                            if (node.IsSelected)
                            {
                                if (node.Tag != null || _values[i].ParameterType.GetElementType() != typeof(string))
                                {
                                    list.Add(node.Tag);
                                }
                                else
                                {
                                    list.Add(node.Name);
                                }
                            }
                            foreach (var n in node.Children)
                                visitor(n);
                        });
                        foreach (var n in tree.RootNodes)
                            visitor(n);
                        var a = Array.CreateInstance(_values[i].ParameterType.GetElementType(), list.Count);
                        for (int index = 0; index < list.Count; index++)
                            a.SetValue(list[index], index);
                        parameters[i] = a;
                        break;
                    default:
                        parameters[i] = _values[i].Value;
                        break;
                }
            }
            return (ISpecification<T>)Activator.CreateInstance(_instanceType, parameters);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ISpecificationBuilder);
        }

        public bool Equals(ISpecificationBuilder other)
        {
            if (null == other) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (_instanceType != other.FilterType) return false;
            return _values.SequenceEqual(other.Parameters);
        }

        public bool Equals(SpecificationBuilder other)
        {
            return this.Equals(other as ISpecificationBuilder);
        }

        public override int GetHashCode()
        {
            return _instanceType.GetHashCode() ^ Name.GetHashCode();
        }
    }
}
