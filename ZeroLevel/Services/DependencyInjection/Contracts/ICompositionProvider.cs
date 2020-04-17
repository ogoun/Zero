namespace ZeroLevel.DependencyInjection
{
    /// <summary>
    /// Provides object composition
    /// </summary>
    public interface ICompositionProvider
    {
        #region Composition

        /// <summary>
        /// Object compositions, insert contract implementation
        /// </summary>
        /// <param name="instanse">Object instance</param>
        void Compose(object instanse, bool recursive = true);

        /// <summary>
        /// Object compositions, insert contract implementation
        /// </summary>
        /// <param name="instanse">Object instance</param>
        /// /// <returns>false if composition fault</returns>
        bool TryCompose(object instanse, bool recursive = true);

        #endregion Composition
    }
}