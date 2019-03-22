namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Интерфейс с методами проведения композиций
    /// </summary>
    public interface ICompositionProvider
    {
        #region Composition
        /// <summary>
        /// Композиция, выполняет подстановку зарегистрированных контрактов в полях и свойствах объекта
        /// </summary>
        /// <param name="instanse">Инстанс объекта</param>
        void Compose(object instanse, bool recursive = true);
        /// <summary>
        /// Безопасная композиция, выполняет подстановку зарегистрированных контрактов в полях и свойствах объекта
        /// </summary>
        /// <param name="instanse">Инстанс объекта</param>
        /// /// <returns>false - при сбое в попытке композиции объекта</returns>
        bool TryCompose(object instanse, bool recursive = true);
        #endregion
    }
}
