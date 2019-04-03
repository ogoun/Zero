using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    /// <summary>
    /// Named configuration sections array
    /// </summary>
    public interface IConfigurationSet :
        IEquatable<IConfigurationSet>,
        IBinarySerializable
    {
        #region Properties

        /// <summary>
        /// Default section, always exists
        /// </summary>
        IConfiguration Default { get; }

        /// <summary>
        /// Get configuration section by name
        /// </summary>
        IConfiguration this[string sectionName] { get; }

        /// <summary>
        /// Get configuration section names
        /// </summary>
        IEnumerable<string> SectionNames { get; }

        /// <summary>
        /// Get all sections
        /// </summary>
        IEnumerable<IConfiguration> Sections { get; }

        /// <summary>
        /// true if changing disallow
        /// </summary>
        bool SectionsFreezed { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Create section
        /// </summary>
        /// <param name="sectionName">Section name</param>
        IConfiguration CreateSection(string sectionName);

        /// <summary>
        /// Get configuration section by name
        /// </summary>
        /// <param name="sectionName">Название секции</param>
        /// <returns>Секция с данными</returns>
        IConfiguration GetSection(string sectionName);

        /// <summary>
        /// Check for a section by name
        /// </summary>
        /// <param name="sectionName">Section name</param>
        bool ContainsSection(string sectionName);

        /// <summary>Remove section by name
        /// </summary>
        /// <param name="sectionName">Section name</param>
        bool RemoveSection(string sectionName);

        /// <summary>
        /// Sets a prohibition on changing configurations
        /// </summary>
        bool FreezeConfiguration(bool permanent = false);

        /// <summary>
        /// Sets a prohibition on changing sections
        /// </summary>
        bool FreezeSections(bool permanent = false);

        /// <summary>
        /// Remove a prohibition on changing configurations
        /// </summary>
        /// <returns>false - если запрет снят</returns>
        bool UnfreezeConfiguration();

        /// <summary>
        /// Sets a prohibition on changing sections
        /// </summary>
        bool UnfreezeSections();

        #endregion Methods
    }
}