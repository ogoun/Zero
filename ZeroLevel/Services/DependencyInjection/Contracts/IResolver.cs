using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Интерфейс с методами разрешений зависимостей
    /// </summary>
    public interface IResolver
    {
        #region Activator
        /// <summary>
        /// Создание экземпляра объекта указанного типа
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <returns>Экземпляр объекта</returns>
        T CreateInstance<T>(string resolveName = "");
        /// <summary>
        /// Создание экземпляра объекта указанного типа
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="args">Аргументы конструктора</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <returns>Экземпляр объекта</returns>
        T CreateInstance<T>(object[] args, string resolveName = "");
        /// <summary>
        /// Создание экземпляра объекта указанного типа
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <returns>Экземпляр объекта</returns>
        object CreateInstance(Type type, string resolveName = "");
        /// <summary>
        /// Создание экземпляра объекта указанного типа
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <returns>Экземпляр объекта</returns>
        object CreateInstance(Type type, object[] args, string resolveName = "");
        #endregion

        #region Resolving
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <typeparam name="T">Тип контракта</typeparam>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        T Resolve<T>(bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <typeparam name="T">Тип контракта</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        T Resolve<T>(string resolveName, bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <typeparam name="T">Тип контракта</typeparam>
        /// <param name="args">Аргументы конструктора</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        T Resolve<T>(object[] args, bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <typeparam name="T">Тип контракта</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        T Resolve<T>(string resolveName, object[] args, bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <param name="type">Тип контракта</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        object Resolve(Type type, bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <param name="type">Тип контракта</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        object Resolve(Type type, string resolveName, bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <param name="type">Тип контракта</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        object Resolve(Type type, object[] args, bool compose = true);
        /// <summary>
        /// Разрешение зависимости
        /// </summary>
        /// <param name="type">Тип контракта</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <param name="compose">Указание провести композицию при построении объектов</param>
        /// <returns>Инстанс</returns>
        object Resolve(Type type, string resolveName, object[] args, bool compose = true);
        #endregion

        #region Safe resolving
        object TryResolve(Type type, out object result, bool compose = true);
        object TryResolve(Type type, object[] args, out object result, bool compose = true);
        object TryResolve(Type type, string resolveName, out object result, bool compose = true);
        bool TryResolve<T>(out T result, bool compose = true);
        bool TryResolve<T>(object[] args, out T result, bool compose = true);
        bool TryResolve<T>(string resolveName, out T result, bool compose = true);
        bool TryResolve<T>(string resolveName, object[] args, out T result, bool compose = true);
        bool TryResolve(Type type, string resolveName, object[] args, out object result, bool compose = true);
        #endregion
    }
}
