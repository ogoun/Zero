using System;

namespace ZeroLevel.DependencyInjection
{
    /// <summary>
    /// Dependency resolver
    /// </summary>
    public interface IResolver
    {
        #region Activator

        /// <summary>
        /// Creating an instance of an object of the specified type
        /// </summary>
        /// <typeparam name="T">Contract or instance type</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <returns>Instance</returns>
        T CreateInstance<T>(string resolveName = "");

        /// <summary>
        /// Creating an instance of an object of the specified type
        /// </summary>
        /// <typeparam name="T">Contract or instance type</typeparam>
        /// <param name="args">Ctor agrs</param>
        /// <param name="resolveName">Dependency name</param>
        /// <returns>Instance</returns>
        T CreateInstance<T>(object[] args, string resolveName = "");

        /// <summary>
        /// Creating an instance of an object of the specified type
        /// </summary>
        /// <param name="type">Contract or instance type</param>
        /// <param name="resolveName">Dependency name</param>
        /// <returns>Instance</returns>
        object CreateInstance(Type type, string resolveName = "");

        /// <summary>
        /// Creating an instance of an object of the specified type
        /// </summary>
        /// <param name="type">Contract or instance type</param>
        /// <param name="args">Ctor agrs</param>
        /// <param name="resolveName">Dependency name</param>
        /// <returns>Instance</returns>
        object CreateInstance(Type type, object[] args, string resolveName = "");

        #endregion Activator

        #region Resolving

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <typeparam name="T">Contract</typeparam>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        T Resolve<T>(bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <typeparam name="T">Contract</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        T Resolve<T>(string resolveName, bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <typeparam name="T">Contract</typeparam>
        /// <param name="args">Ctor agrs</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        T Resolve<T>(object[] args, bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <typeparam name="T">Contract</typeparam>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="args">Ctor agrs</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        T Resolve<T>(string resolveName, object[] args, bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <param name="type">Contract</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        object Resolve(Type type, bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <param name="type">Contract</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        object Resolve(Type type, string resolveName, bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <param name="type">Contract</param>
        /// <param name="args">Ctor agrs</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        object Resolve(Type type, object[] args, bool compose = true);

        /// <summary>
        /// Dependency resolve
        /// </summary>
        /// <param name="type">Contract</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="args">Ctor agrs</param>
        /// <param name="compose">Compose the object when true</param>
        /// <returns>Instance</returns>
        object Resolve(Type type, string resolveName, object[] args, bool compose = true);

        #endregion Resolving

        #region Safe resolving

        object TryResolve(Type type, out object result, bool compose = true);

        object TryResolve(Type type, object[] args, out object result, bool compose = true);

        object TryResolve(Type type, string resolveName, out object result, bool compose = true);

        bool TryResolve<T>(out T result, bool compose = true);

        bool TryResolve<T>(object[] args, out T result, bool compose = true);

        bool TryResolve<T>(string resolveName, out T result, bool compose = true);

        bool TryResolve<T>(string resolveName, object[] args, out T result, bool compose = true);

        bool TryResolve(Type type, string resolveName, object[] args, out object result, bool compose = true);

        #endregion Safe resolving
    }
}