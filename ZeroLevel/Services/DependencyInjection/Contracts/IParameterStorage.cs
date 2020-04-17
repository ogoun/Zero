using System;

namespace ZeroLevel.DependencyInjection
{
    /// <summary>
    /// DI parameters stogare (string key and anytype value)
    /// </summary>
    public interface IParameterStorage
    {
        #region IEverythingStorage

        /// <summary>
        /// Save parameter
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Parameter value</param>
        void Save<T>(string key, T value);

        /// <summary>
        /// Save or update parameter
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Parameter value</param>
        void SaveOrUpdate<T>(string key, T value);

        /// <summary>
        /// Safe save parameter
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Parameter value</param>
        bool TrySave<T>(string key, T value);

        /// <summary>
        /// Remove parameter by key
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        void Remove<T>(string key);

        /// <summary>
        /// Safe remove parameter by key
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        bool TryRemove<T>(string key);

        /// <summary>
        /// Get parameter value by key
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        /// <returns>Parameter value</returns>
        T Get<T>(string key);

        T GetOrDefault<T>(string key);

        T GetOrDefault<T>(string key, T defaultValue);

        /// <summary>
        /// Get parameter value by key
        /// </summary>
        /// <param name="type">Parameter type</param>
        /// <param name="key">Key</param>
        /// <returns>Parameter value</returns>
        object Get(Type type, string key);

        /// <summary>
        /// Check for parameter existence by key
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="key">Key</param>
        bool Contains<T>(string key);

        #endregion IEverythingStorage
    }
}