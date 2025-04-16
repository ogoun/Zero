namespace ZeroLevel.Sleopok.Engine.Services.Storage
{
    /// <summary>
    /// Мета
    /// </summary>
    public sealed class StoreMetadata
    {
        public StoreMetadata(string field) => Field = field;

        /// <summary>
        /// Поле документа
        /// </summary>
        public string Field { get; set; }
    }
}
