using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Интерфейс с методами регистрации зависимостей с явным указанием экземпляров
    /// (синглтон)
    /// </summary>
    public interface IContainerInstanceRegister
    {
        #region Register instance
        /// <summary>
        /// Регистрация готового экземпляра (синглтон)
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <param name="implementation">Экземпляр</param>
        void Register<TContract>(TContract implementation);
        /// <summary>
        /// Регистрация готового экземпляра (синглтон)
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <param name="implementation">Экземпляр</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        void Register<TContract>(TContract implementation, string resolveName);
        /// <summary>
        /// Регистрация готового экземпляра (синглтон)
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="implementation">Экземпляр</param>
        void Register(Type contractType, object implementation);
        /// <summary>
        /// Регистрация готового экземпляра (синглтон)
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="implementation">Экземпляр</param>
        void Register(Type contractType, string resolveName, object implementation);
        #endregion        
    }
}
