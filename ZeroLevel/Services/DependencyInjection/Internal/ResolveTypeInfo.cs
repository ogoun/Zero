using System;
using System.Collections.Generic;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Метаданные разрешения зависимости
    /// </summary>
    internal sealed class ResolveTypeInfo
    {
        /// <summary>
        /// Разрешение зависимости по умолчанию
        /// </summary>
        public bool IsDefault;
        /// <summary>
        /// Создается только один экземпляр (в случае true)
        /// </summary>
        public bool IsShared;
        /// <summary>
        /// Тип инстанса (в том числе обобщенный)
        /// </summary>
        public Type ImplementationType;
        /// <summary>
        /// Ключ определения зависимости
        /// </summary>
        public string ResolveKey;
        /// <summary>
        /// Кэш экземпляра
        /// </summary>
        public object SharedInstance;
        /// <summary>
        /// Кэш обобщенных типов
        /// </summary>
        public Dictionary<Type, Type> GenericCachee;
        /// <summary>
        /// Кэш обобщенных экземпляров
        /// </summary>
        public Dictionary<Type, object> GenericInstanceCachee;
        /// <summary>
        /// Параметры конструктора объекта
        /// </summary>
        public object[] ConstructorParameters;
    }
}
