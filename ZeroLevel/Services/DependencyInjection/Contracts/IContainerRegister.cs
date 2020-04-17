using System;

namespace ZeroLevel.DependencyInjection
{
    /// <summary>
    /// Methods for register contact resolvings
    /// (singletones and multitones)
    /// </summary>
    public interface IContainerRegister : IDisposable
    {
        #region Register

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        void Register<TContract, TImplementation>();

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        void Register<TContract, TImplementation>(string resolveName);

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="shared">true - for singletone</param>
        void Register<TContract, TImplementation>(bool shared);

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        void Register<TContract, TImplementation>(string resolveName, bool shared);

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        void Register(Type contractType, Type implementationType);

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        void Register(Type contractType, Type implementationType, string resolveName);

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="shared">true - for singletone</param>
        void Register(Type contractType, Type implementationType, bool shared);

        /// <summary>
        /// Dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        void Register(Type contractType, Type implementationType, string resolveName, bool shared);

        #endregion Register

        #region Register with parameters

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister<TContract, TImplementation>(object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister<TContract, TImplementation>(string resolveName, object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister<TContract, TImplementation>(bool shared, object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister<TContract, TImplementation>(string resolveName, bool shared, object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister(Type contractType, Type implementationType, object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister(Type contractType, Type implementationType, string resolveName, object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister(Type contractType, Type implementationType, bool shared, object[] constructorParameters);

        /// <summary>
        /// Dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        void ParameterizedRegister(Type contractType, Type implementationType, string resolveName, bool shared, object[] constructorParameters);

        #endregion Register with parameters

        #region Safe register

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister<TContract, TImplementation>(Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister<TContract, TImplementation>(string resolveName, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="shared">true - for singletone</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister<TContract, TImplementation>(bool shared, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister<TContract, TImplementation>(string resolveName, bool shared, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister(Type contractType, Type implementationType, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister(Type contractType, Type implementationType, string resolveName, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister(Type contractType, Type implementationType, bool shared, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryRegister(Type contractType, Type implementationType, string resolveName, bool shared, Action<Exception> fallback = null);

        #endregion Safe register

        #region Safe register with parameters

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(string resolveName, object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(bool shared, object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <typeparam name="TImplementation">Dependency resolution</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister<TContract, TImplementation>(string resolveName, bool shared, object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, string resolveName, object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, bool shared, object[] constructorParameters, Action<Exception> fallback = null);

        /// <summary>
        /// Safe dependency resolution registration with constructor parameters
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementationType">Dependency resolution</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="shared">true - for singletone</param>
        /// <param name="constructorParameters">Ctor args</param>
        /// <param name="fallback">Error handler</param>
        /// <returns>true - registration successfully completed</returns>
        bool TryParameterizedRegister(Type contractType, Type implementationType, string resolveName, bool shared, object[] constructorParameters, Action<Exception> fallback = null);

        #endregion Safe register with parameters
    }
}