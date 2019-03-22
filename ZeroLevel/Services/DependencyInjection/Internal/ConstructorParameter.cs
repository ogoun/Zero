using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Тип аргумента конструктора в контексте иньекции зависимостей
    /// </summary>
    internal enum ConstructorParameterKind
    {
        /// <summary>
        /// Аргумент задается из переданных значений
        /// </summary>
        None,
        /// <summary>
        /// Аргумент задается из параметров контейнера
        /// </summary>
        Parameter,
        /// <summary>
        /// Аргумент задается разрешением зависимости
        /// </summary>
        Resolve
    }

    /// <summary>
    /// Метаданные для описания аргумента конструктора
    /// </summary>
    internal sealed class ConstructorParameter
    {
        /// <summary>
        /// Тип аргумента в рамках DI
        /// </summary>
        public ConstructorParameterKind ParameterKind;
        /// <summary>
        /// Тип для определения аргумента через DI
        /// </summary>
        public Type ParameterResolveType;
        /// <summary>
        /// Имя для определения аргумента через DI
        /// </summary>
        public string ParameterResolveName;
        /// <summary>
        /// Флаг определяющий допустимость записи null в качестве значения аргумента
        /// </summary>
        public bool IsNullable;
        /// <summary>
        /// Тип аргумента
        /// </summary>
        public Type Type;
    }
}
