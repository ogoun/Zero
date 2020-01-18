namespace Lemmatization
{
    public enum TokenType
    {
        Unknown,
        /// <summary>
        /// Знак пукнтуации
        /// </summary>
        Punctuation,
        /// <summary>
        /// Аббревиатура
        /// </summary>
        Аbbreviation,
        /// <summary>
        /// Слово
        /// </summary>
        Word,
        /// <summary>
        /// Идентификатор (может содержать не только буквы)
        /// </summary>
        Identity,
        /// <summary>
        /// Число
        /// </summary>
        Number,
        /// <summary>
        /// Номер телефона
        /// </summary>
        PhoneNumber,
        /// <summary>
        /// Ссылка
        /// </summary>
        Link
    }
}
