using System;
using System.Collections.Generic;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Dependency resolving metadata
    /// </summary>
    internal sealed class ResolveTypeInfo
    {
        /// <summary>
        /// Default - without dependency name
        /// </summary>
        public bool IsDefault;
        /// <summary>
        /// Singletone when true
        /// </summary>
        public bool IsShared;
        /// <summary>
        /// Instance type (may be generic)
        /// </summary>
        public Type ImplementationType;
        /// <summary>
        /// Dependency resolving key
        /// </summary>
        public string ResolveKey;
        /// <summary>
        /// Instance cache
        /// </summary>
        public object SharedInstance;
        /// <summary>
        /// Generic types cahce
        /// </summary>
        public Dictionary<Type, Type> GenericCachee;
        /// <summary>
        /// Generic instances cahce
        /// </summary>
        public Dictionary<Type, object> GenericInstanceCachee;
        /// <summary>
        /// Constructor parameters
        /// </summary>
        public object[] ConstructorParameters;
    }
}
