using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    /// <summary>
    /// Интерфейс конфигурационных данных
    /// </summary>
    public interface IConfiguration :
        IEquatable<IConfiguration>,
        IBinarySerializable
    {
        #region Properties
        /// <summary>
        /// Получение списка значений по ключу
        /// </summary>
        IEnumerable<string> this[string key] { get; }
        /// <summary>
        /// Перечисление ключей
        /// </summary>
        IEnumerable<string> Keys { get; }
        /// <summary>
        /// Указывает что конфигурация заблокирована на изменения
        /// </summary>
        bool Freezed { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Получение списка значений параметра по ключу
        /// </summary>
        /// <param name="key">Имя параметра</param>
        /// <returns>Список значений</returns>
        IEnumerable<string> Items(string key);
        /// <summary>
        /// Получение одного(первого) значения параметра по ключу
        /// </summary>
        string First(string key);
        /// <summary>
        /// Получить первое значение в виде объекта типа T
        /// </summary>
        T First<T>(string key);
        /// <summary>
        /// Получить первое значение или значение по умолчанию
        /// </summary>
        string FirstOrDefault(string name, string defaultValue);
        /// <summary>
        /// Получить первое значение в виде объекта типа T или получить значение по умолчанию
        /// </summary>
        T FirstOrDefault<T>(string name);
        /// <summary>
        /// Получить первое значение в виде объекта типа T или получить переданное значение по умолчанию
        /// </summary>
        T FirstOrDefault<T>(string name, T defaultValue);
        /// <summary>
        /// Проверка наличия ключа
        /// </summary>
        bool Contains(string key);
        /// <summary>
        /// Проверка наличия одного из ключей
        /// </summary>
        bool Contains(params string[] keys);
        /// <summary>
        /// Проверка наличия значения по ключу
        /// </summary>
        bool ContainsValue(string key, string value);
        /// <summary>
        /// Количество значений параметра
        /// </summary>
        int Count(string key);
        /// <summary>
        /// Выполняет указанное действие только в случае если в конфигурации есть ключ
        /// </summary>
        void DoWithFirst(string key, Action<string> action);
        /// <summary>
        /// Выполняет указанное действие только в случае если в конфигурации есть ключ
        /// </summary>
        void DoWithFirst<T>(string key, Action<T> action);
        #endregion

        #region Create, Clean, Delete
        /// <summary>
        /// Очистка всей секции
        /// </summary>
        IConfiguration Clear();
        /// <summary>
        /// Очистка значения ключа
        /// </summary>
        IConfiguration Clear(string key);
        /// <summary>
        /// Удаление ключа и значений
        /// </summary>
        IConfiguration Remove(string key);
        /// <summary>
        /// Добавление параметра
        /// </summary>
        IConfiguration Append(string key, string value);
        /// <summary>
        /// Задает значение в единственном числе,
        /// существующее значение будет перезаписано
        /// </summary>
        IConfiguration SetUnique(string key, string value);
        /// <summary>
        /// Запрещает вносить какие-либо изменения в конфигурацию
        /// </summary>
        /// <returns>false - если уже установлен запрет</returns>
        bool Freeze(bool permanent = false);
        /// <summary>
        /// Убирает запрет на внесение изменений в конфигурацию
        /// </summary>
        /// <returns>false - если запрет снят</returns>
        bool Unfreeze();
        #endregion
    }
}
