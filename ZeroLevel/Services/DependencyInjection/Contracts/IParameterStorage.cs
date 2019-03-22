using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Интерфейс с методами для реализации хранения параметров
    /// (хранилище ключ-значение, где в качестве ключа используется сущность типа string, а в качестве значения объект любого типа)
    /// </summary>
    public interface IParameterStorage
    {
        #region IEverythingStorage
        /// <summary>
        /// Сохранение параметра
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение параметра</param>
        void Save<T>(string key, T value);
        /// <summary>
        /// Сохранение или обновление параметра
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение параметра</param>
        void SaveOrUpdate<T>(string key, T value);
        /// <summary>
        /// Безопасное сохранение параметра
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение параметра</param>
        /// <returns>true - в случае успеха</returns>
        bool TrySave<T>(string key, T value);
        /// <summary>
        /// Удаление параметра
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        void Remove<T>(string key);
        /// <summary>
        /// Безопасное удаление параметра
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <returns>true - в случае успеха</returns>
        bool TryRemove<T>(string key);
        /// <summary>
        /// Запрос сохраненного параметра
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <returns>Значение параметра</returns>
        T Get<T>(string key);
        T GetOrDefault<T>(string key);
        T GetOrDefault<T>(string key, T defaultValue);
        /// <summary>
        /// Запрос сохраненного параметра
        /// </summary>
        /// <param name="type">Тип параметра</param>
        /// <param name="key">Ключ</param>
        /// <returns>Значение параметра</returns>
        object Get(Type type, string key);
        /// <summary>
        /// Проверка наличия параметра с указанным именем
        /// </summary>
        /// <typeparam name="T">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <returns>Указывает наличие параметра с заданным именем</returns>
        bool Contains<T>(string key);
        #endregion
    }
}
