using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Интерфейс с методами регистрации зависимостей с указанием типов контракта и зависимости
    /// (синглтоны и мультитоны)
    /// </summary>
    public interface IContainerRegister : IDisposable
    {
        #region Register
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        void Register<TContract, TImplementation>();
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        void Register<TContract, TImplementation>(string resolveName);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="shared">true - для синглтонов</param>
        void Register<TContract, TImplementation>(bool shared);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        void Register<TContract, TImplementation>(string resolveName, bool shared);

        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        void Register(Type contractType, Type implementationType);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        void Register(Type contractType, Type implementationType, string resolveName);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="shared">true - для синглтонов</param>
        void Register(Type contractType, Type implementationType, bool shared);
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        void Register(Type contractType, Type implementationType, string resolveName, bool shared);
        #endregion

        #region Register with parameters
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister<TContract, TImplementation>(object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister<TContract, TImplementation>(string resolveName, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister<TContract, TImplementation>(bool shared, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister<TContract, TImplementation>(string resolveName, bool shared, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister(Type contractType, Type implementationType, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister(Type contractType, Type implementationType, string resolveName, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister(Type contractType, Type implementationType, bool shared, object[] constructorParameters);
        /// <summary>
        /// Регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        void ParameterizedRegister(Type contractType, Type implementationType, string resolveName, bool shared, object[] constructorParameters);
        #endregion

        #region Safe register
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister<TContract, TImplementation>(Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister<TContract, TImplementation>(string resolveName, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister<TContract, TImplementation>(bool shared, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister<TContract, TImplementation>(string resolveName, bool shared, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister(Type contractType, Type implementationType, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister(Type contractType, Type implementationType, string resolveName, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister(Type contractType, Type implementationType, bool shared, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryRegister(Type contractType, Type implementationType, string resolveName, bool shared, Action<Exception> fallback = null);
        #endregion

        #region Safe register with parameters
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(string resolveName, object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(bool shared, object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <typeparam name="TImplementation">Тип разрешения</typeparam>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(string resolveName, bool shared, object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, string resolveName, object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, bool shared, object[] constructorParameters, Action<Exception> fallback = null);
        /// <summary>
        /// Безопасная регистрация разрешения зависимости с указанием параметров конструктора
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementationType">Тип разрешения</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="shared">true - для синглтонов</param>
        /// <param name="constructorParameters">Аргументы конструктора</param>
        /// <param name="fallback">Обработчик исключения</param>
        /// <returns>true - в случае успешной регистрации</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, string resolveName, bool shared, object[] constructorParameters, Action<Exception> fallback = null);
        #endregion
    }
}
