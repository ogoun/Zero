using System;

namespace ZeroLevel.DependencyInjection
{
    /// <summary>
    /// Constructor argument type
    /// </summary>
    internal enum ConstructorParameterKind
    {
        /// <summary>
        /// Constant
        /// </summary>
        None,

        /// <summary>
        /// DI parameter
        /// </summary>
        Parameter,

        /// <summary>
        /// Dependency
        /// </summary>
        Dependency
    }

    /// <summary>
    /// Constructor argument metadata
    /// </summary>
    internal sealed class ConstructorParameter
    {
        /// <summary>
        /// Argument DI-type
        /// </summary>
        public ConstructorParameterKind ParameterKind;

        /// <summary>
        /// Argument contract type
        /// </summary>
        public Type ParameterResolveType;

        /// <summary>
        /// Dependency name
        /// </summary>
        public string ParameterResolveName;

        /// <summary>
        /// Allow null
        /// </summary>
        public bool IsNullable;

        /// <summary>
        /// Argument CLR type
        /// </summary>
        public Type Type;
    }
}