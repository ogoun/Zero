using System;

namespace ZeroLevel.Contracts.Specification.Building
{
    /// <summary>
    /// Specification constructor parameter
    /// </summary>
    public class SpecificationParameter
    {
        /// <summary>
        /// Display Name
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Parameter name
        /// </summary>
        public string ParameterName;
        /// <summary>
        /// Parameter type
        /// </summary>
        public Type ParameterType;
        /// <summary>
        /// Parameter value
        /// </summary>
        public object Value;
    }
}
