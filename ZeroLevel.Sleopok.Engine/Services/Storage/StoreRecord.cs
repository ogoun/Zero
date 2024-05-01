namespace ZeroLevel.Sleopok.Engine.Services.Storage
{
    public sealed class StoreRecord
    {
        /// <summary>
        /// Токен / ключ
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Идентификаторы документов / значение
        /// </summary>
        public string[] Documents { get; set; }
    }
}
