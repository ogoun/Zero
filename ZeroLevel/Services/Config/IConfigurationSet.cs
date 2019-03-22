using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    /// <summary>
    /// Интерфейс набора конфигурационных данных
    /// </summary>
    public interface IConfigurationSet :
        IEquatable<IConfigurationSet>,
        IBinarySerializable
    {
        #region Properties
        /// <summary>
        /// Получение конфигурации по умолчанию
        /// </summary>
        IConfiguration Default { get; }
        /// <summary>
        /// Получение конфигурации по имени
        /// </summary>
        IConfiguration this[string sectionName] { get; }
        /// <summary>
        /// Получение имен конфигураций
        /// </summary>
        IEnumerable<string> SectionNames { get; }
        /// <summary>
        /// Получение всех конфигураций
        /// </summary>
        IEnumerable<IConfiguration> Sections { get; }
        /// <summary>
        /// Указывает, заблокирован или нет набор секций
        /// </summary>
        bool SectionsFreezed { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Создание секции параметров
        /// </summary>
        /// <param name="sectionName">Название секции</param>
        IConfiguration CreateSection(string sectionName);
        /// <summary>
        /// Запрос секции данных по имени секции
        /// </summary>
        /// <param name="sectionName">Название секции</param>
        /// <returns>Секция с данными</returns>
        IConfiguration GetSection(string sectionName);
        /// <summary>
        /// Проверка наличия секции с указанным именем
        /// </summary>
        /// <param name="sectionName">Название секции</param>
        /// <returns>true - секция существует</returns>
        bool ContainsSection(string sectionName);
        /// <summary>
        /// Удаление секции
        /// </summary>
        /// <param name="sectionName">Название секции</param>
        /// <returns>false - если секция уже удалена или не существует</returns>
        bool RemoveSection(string sectionName);
        /// <summary>
        /// Запрещает вносить какие-либо изменения в существующую конфигурацию во всех секциях
        /// а также менять набор секций
        /// </summary>
        /// <returns>false - если уже установлен запрет</returns>
        bool FreezeConfiguration(bool permanent = false);
        /// <summary>
        /// Запрещает вносить какие-либо изменения в существующий набор секций
        /// </summary>
        /// <returns>false - если уже установлен запрет</returns>
        bool FreezeSections(bool permanent = false);
        /// <summary>
        /// Убирает запрет на внесение изменений в существующую конфигурацию во всех секциях
        /// а также разрешает менять набор секций
        /// </summary>
        /// <returns>false - если запрет снят</returns>
        bool UnfreezeConfiguration();
        /// <summary>
        /// Убирает запрет на внесение изменений в существующий набор секций
        /// </summary>
        /// <returns>false - если запрет снят</returns>
        bool UnfreezeSections();
        #endregion
    }
}
