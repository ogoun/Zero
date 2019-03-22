using System;

namespace ZeroLevel.Contracts.Specification.Building
{
    /// <summary>
    /// Параметр конструктора спецификации
    /// </summary>
    public class SpecificationParameter
    {
        /// <summary>
        /// Отображаемое имя
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Имя параметра
        /// </summary>
        public string ParameterName;
        /// <summary>
        /// Тип параметра
        /// </summary>
        public Type ParameterType;
        /// <summary>
        /// Значение параметра
        /// </summary>
        public object Value;
    }
}
