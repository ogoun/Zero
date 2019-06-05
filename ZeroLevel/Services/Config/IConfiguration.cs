using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    /// <summary>
    /// Configuration section
    /// </summary>
    public interface IConfiguration :
        IEquatable<IConfiguration>,
        IBinarySerializable
    {
        #region Properties

        /// <summary>
        /// Get values by key
        /// </summary>
        IEnumerable<string> this[string key] { get; }

        /// <summary>
        /// Keys
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// Configuration is locked for change when true
        /// </summary>
        bool Freezed { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Get values by key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Values list</returns>
        IEnumerable<string> Items(string key);

        /// <summary>
        /// Get first value by key
        /// </summary>
        string First(string key);

        /// <summary>
        /// Get first value by key with cast to <typeparamref name="T"/>
        /// </summary>
        T First<T>(string key);

        /// <summary>
        /// Get first or default value by key
        /// </summary>
        string FirstOrDefault(string name, string defaultValue);

        /// <summary>
        /// Get first or default value by key with cast to <typeparamref name="T"/>
        /// </summary>
        T FirstOrDefault<T>(string name);

        /// <summary>
        /// Get first or default value by key with cast to <typeparamref name="T"/>
        /// </summary>
        T FirstOrDefault<T>(string name, T defaultValue);

        /// <summary>
        /// Check key exists
        /// </summary>
        bool Contains(string key);

        /// <summary>
        /// Check one of key exists
        /// </summary>
        bool Contains(params string[] keys);

        /// <summary>
        /// true if exists one or more values by key
        /// </summary>
        bool ContainsValue(string key, string value);

        /// <summary>
        /// Count values by key
        /// </summary>
        int Count(string key);

        /// <summary>
        /// Do action if key exists, action takes first value
        /// </summary>
        void DoWithFirst(string key, Action<string> action);

        /// <summary>
        /// Do action if key exists, action takes first value with cast to <typeparamref name="T"/>
        /// </summary>
        void DoWithFirst<T>(string key, Action<T> action);

        #endregion Methods

        #region Create, Clean, Delete

        /// <summary>
        /// Clean
        /// </summary>
        IConfiguration Clear();

        /// <summary>
        /// Clean values by key
        /// </summary>
        IConfiguration Clear(string key);

        /// <summary>
        /// Remove key and binded values
        /// </summary>
        IConfiguration Remove(string key);

        /// <summary>
        /// Append key and value
        /// </summary>
        IConfiguration Append(string key, string value);

        /// <summary>
        /// Set key with one value, if any values by key exists, they will be dropped
        /// </summary>
        IConfiguration SetUnique(string key, string value);

        /// <summary>
        /// Sets a prohibition on changing
        /// </summary>
        /// <returns>false - prohibition was set already</returns>
        bool Freeze(bool permanent = false);

        /// <summary>
        /// Remove a prohibition on changing
        /// </summary>
        bool Unfreeze();

        #endregion Create, Clean, Delete

        void CopyTo(IConfiguration config);
    }
}