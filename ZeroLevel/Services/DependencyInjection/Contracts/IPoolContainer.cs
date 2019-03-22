using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Интерфейс с методами для реализации паттерна пул объектов в контейнере
    /// </summary>
    public interface IPoolContainer
    {
        #region Register poolable dependencies
        /// <summary>
        /// Регистрация пула
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <param name="initialCount">Начальное количество объектов в пуле</param>
        /// <param name="capacity">Максимальное количество объектов в пуле (при -1 пул не ограничен)</param>
        void RegisterPool<TContract>(int initialCount, int capacity);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        void RegisterPoolable<TContract, TImplementation>();
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        void RegisterPoolable<TContract, TImplementation>(string resolveName);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        void RegisterPoolable(Type contractType, Type implementationType);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        void RegisterPoolable(Type contractType, Type implementationType, string resolveName);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <param name="implementation">Экземпляр</param>
        void RegisterPoolable<TContract>(TContract implementation);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementation">Экземпляр</param>
        void RegisterPoolable(Type contractType, object implementation);
        #endregion

        #region Register poolable parametrizied dependencies
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void RegisterPoolableParametrizied<TContract, TImplementation>(object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void RegisterPoolableParametrizied<TContract, TImplementation>(string resolveName, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void RegisterPoolableParametrizied(Type contractType, Type implementationType, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void RegisterPoolableParametrizied(Type contractType, Type implementationType, string resolveName, object[] constructorParameters);
        #endregion
    }
}
