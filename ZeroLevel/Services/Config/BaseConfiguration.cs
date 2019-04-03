using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using ZeroLevel.Services.Reflection;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Config
{
    /// <summary>
    /// Base configuration
    /// </summary>
    internal sealed class BaseConfiguration :
        IConfiguration
    {
        #region Private members

        /// <summary>
        /// When true, any changes disallow
        /// </summary>
        private bool _freezed = false;

        /// <summary>
        /// When true, freeze permanent, can't be canceled
        /// </summary>
        private bool _permanentFreezed = false;

        private readonly object _freezeLock = new object();

        /// <summary>
        /// Key-values dictionary
        /// </summary>
        private readonly ConcurrentDictionary<string, IList<string>> _keyValues = new ConcurrentDictionary<string, IList<string>>();

        /// <summary>
        /// Empty list
        /// </summary>
        private static readonly IEnumerable<string> EmptyValuesList = new List<string>(0);

        private static string GetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            return key.Trim().ToLower(CultureInfo.InvariantCulture);
        }

        #endregion Private members

        #region Properties

        /// <summary>
        /// Get values by key
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Values list</returns>
        public IEnumerable<string> this[string key]
        {
            get
            {
                key = key.ToLower(CultureInfo.CurrentCulture);
                IList<string> result;
                if (_keyValues.TryGetValue(key, out result))
                {
                    return result;
                }
                return EmptyValuesList;
            }
        }

        /// <summary>
        /// Keys  list
        /// </summary>
        public IEnumerable<string> Keys
        {
            get { return _keyValues.Keys; }
        }

        public bool Freezed
        {
            get
            {
                return _freezed;
            }
        }

        #endregion Properties

        #region Public methods

        #region Get

        /// <summary>
        /// Получение списка значение соотвествующих указанному ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Список значений</returns>
        public IEnumerable<string> Items(string key)
        {
            return this[key];
        }

        /// <summary>
        /// Получение первого значения для указанного ключа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Первое значение, или null если ключ есть, но нет значений, или KeyNotFoundException если нет ключа</returns>
        public string First(string key)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                    return result[0];
                return null;
            }
            throw new KeyNotFoundException("Key not found: " + key);
        }

        public void DoWithFirst(string key, Action<string> action)
        {
            if (Contains(key))
            {
                action(First(key));
            }
        }

        public void DoWithFirst<T>(string key, Action<T> action)
        {
            if (Contains(key))
            {
                action(First<T>(key));
            }
        }

        /// <summary>
        /// Получение первого значения для указанного ключа, с попыткой преобразования в указанный тип
        /// </summary>
        /// <typeparam name="T">Ожидаемый тип</typeparam>
        /// <param name="key">Ключ</param>
        /// <returns>Первое значение, или default(T) если ключ есть, но нет значений, или KeyNotFoundException если нет ключа</returns>
        public T First<T>(string key)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                    return (T)StringToTypeConverter.TryConvert(result[0], typeof(T));
                return default(T);
            }
            throw new KeyNotFoundException("Parameter not found: " + key);
        }

        /// <summary>
        /// Получение первого значения для указанного ключа, или значения по умолчанию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        /// <returns>Первое значение, или значение по умолчанию если нет значений или ключа</returns>
        public string FirstOrDefault(string key, string defaultValue)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                    return result[0];
            }
            return defaultValue;
        }

        /// <summary>
        /// Получение первого значения для указанного ключа, или значения по умолчанию, с попыткой преобразования в указанный тип
        /// </summary>
        /// <typeparam name="T">Ожидаемый тип</typeparam>
        /// <param name="key">Ключ</param>
        /// <returns>Первое значение, или default(T) если нет значений или ключа</returns>
        public T FirstOrDefault<T>(string key)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                    return (T)StringToTypeConverter.TryConvert(result[0], typeof(T));
            }
            return default(T);
        }

        /// <summary>
        /// Получение первого значения для указанного ключа, или значения по умолчанию, с попыткой преобразования в указанный тип
        /// </summary>
        /// <typeparam name="T">Ожидаемый тип</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        /// <returns>Первое значение, или значение по умолчанию если нет значений или ключа</returns>
        public T FirstOrDefault<T>(string key, T defaultValue)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                    return (T)StringToTypeConverter.TryConvert(result[0], typeof(T));
            }
            return defaultValue;
        }

        /// <summary>
        /// Проверка наличия ключа и непустого списка связанных с ним значений
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>true - если существует ключ и есть хотя бы одно значение</returns>
        public bool Contains(string key)
        {
            key = GetKey(key);
            return _keyValues.ContainsKey(key) && _keyValues[key].Count > 0;
        }

        /// <summary>
        /// Проверка наличия одного из ключей
        /// </summary>
        public bool Contains(params string[] keys)
        {
            foreach (var key in keys)
                if (Contains(key)) return true;
            return false;
        }

        /// <summary>
        /// Проверка наличия ключа и связанного с ним значения
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsValue(string key, string value)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                return result.Contains(value);
            }
            return false;
        }

        /// <summary>
        /// Количество значений связанных с указанным ключом
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Количество значений</returns>
        public int Count(string key)
        {
            key = GetKey(key);
            if (_keyValues.ContainsKey(key))
            {
                return _keyValues[key].Count;
            }
            return 0;
        }

        #endregion Get

        /// <summary>
        /// Add key-value
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public IConfiguration Append(string key, string value)
        {
            if (false == _freezed)
            {
                key = GetKey(key);
                if (false == _keyValues.ContainsKey(key))
                {
                    _keyValues.TryAdd(key, new List<string>());
                }
                _keyValues[key].Add(value?.Trim() ?? null);
            }
            return this;
        }

        /// <summary>
        /// Set unique value for key
        /// </summary>
        public IConfiguration SetUnique(string key, string value)
        {
            if (false == _freezed)
            {
                key = GetKey(key);
                if (false == _keyValues.ContainsKey(key))
                {
                    _keyValues.TryAdd(key, new List<string>());
                }
                else
                {
                    _keyValues[key].Clear();
                }
                _keyValues[key].Add(value?.Trim() ?? null);
            }
            return this;
        }

        /// <summary>
        /// Clean values binded with key
        /// </summary>
        /// <param name="key">Key</param>
        public IConfiguration Clear(string key)
        {
            if (false == _freezed)
            {
                key = GetKey(key);
                if (_keyValues.ContainsKey(key))
                {
                    _keyValues[key].Clear();
                }
            }
            return this;
        }

        /// <summary>
        /// Configuration drop
        /// </summary>
        public IConfiguration Clear()
        {
            if (false == _freezed)
            {
                _keyValues.Clear();
            }
            return this;
        }

        /// <summary>
        /// Remove key and binded values
        /// </summary>
        /// <param name="key">Key</param>
        public IConfiguration Remove(string key)
        {
            if (false == _freezed)
            {
                IList<string> removed;
                _keyValues.TryRemove(GetKey(key), out removed);
            }
            return this;
        }

        public bool Freeze(bool permanent = false)
        {
            lock (_freezeLock)
            {
                if (false == _freezed)
                {
                    _freezed = true;
                    _permanentFreezed = permanent;
                    return true;
                }
                else if (_permanentFreezed == false && permanent)
                {
                    _permanentFreezed = true;
                    return true;
                }
                return false;
            }
        }

        public bool Unfreeze()
        {
            lock (_freezeLock)
            {
                if (_freezed && _permanentFreezed == false)
                {
                    _freezed = false;
                    return true;
                }
                return false;
            }
        }

        #endregion Public methods

        #region IEquatable

        public bool Equals(IConfiguration other)
        {
            if (other == null)
            {
                return false;
            }
            if (this.Keys.NoOrderingEquals(other.Keys) == false)
            {
                return false;
            }
            foreach (var key in Keys)
            {
                if (this[key].NoOrderingEquals(other[key]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion IEquatable

        #region Binary Serializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(this._freezed);
            writer.WriteBoolean(this._permanentFreezed);
            writer.WriteInt32(_keyValues.Count);
            foreach (var pair in _keyValues)
            {
                writer.WriteString(pair.Key);
                writer.WriteInt32(pair.Value.Count);
                foreach (var value in pair.Value)
                {
                    writer.WriteString(value);
                }
            }
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._freezed = reader.ReadBoolean();
            this._permanentFreezed = reader.ReadBoolean();
            var count = reader.ReadInt32();
            _keyValues.Clear();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var count_values = reader.ReadInt32();
                var list_values = new List<string>();
                for (var k = 0; k < count_values; k++)
                {
                    list_values.Add(reader.ReadString());
                }
                _keyValues.TryAdd(key, list_values);
            }
        }

        #endregion Binary Serializable
    }
}