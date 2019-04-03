using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Methods for register contact implementations
    /// (singletone)
    /// </summary>
    public interface IContainerInstanceRegister
    {
        #region Register instance

        /// <summary>
        /// Register instance for contract <typeparamref name="TContract"/>. (singletone)
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <param name="implementation">Instance</param>
        void Register<TContract>(TContract implementation);

        /// <summary>
        /// Register instance for contract <typeparamref name="TContract"/>. (singletone)
        /// </summary>
        /// <typeparam name="TContract">Contract</typeparam>
        /// <param name="implementation">Instance</param>
        /// <param name="resolveName">Dependency name</param>
        void Register<TContract>(TContract implementation, string resolveName);

        /// <summary>
        /// Register instance for contract (singletone)
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="implementation">Instance</param>
        void Register(Type contractType, object implementation);

        /// <summary>
        /// Register instance for contract (singletone)
        /// </summary>
        /// <param name="contractType">Contract</param>
        /// <param name="resolveName">Dependency name</param>
        /// <param name="implementation">Instance</param>
        void Register(Type contractType, string resolveName, object implementation);

        #endregion Register instance

    }
}