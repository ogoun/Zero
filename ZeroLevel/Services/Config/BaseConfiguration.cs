using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using ZeroLevel.Services.Collections;
using ZeroLevel.Services.ObjectMapping;
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
        /// Getting a list of the value corresponding to the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Values list</returns>
        public IEnumerable<string> Items(string key)
        {
            return this[key];
        }

        /// <summary>
        /// Getting the first value for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The first value, or null if the key is, but there are no values, or KeyNotFoundException if there is no key</returns>
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
        /// Getting the first value for the specified key, with an attempt to convert to the specified type
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="key">Key</param>
        /// <returns>The first value, or default (T) if there is a key but no values, or KeyNotFoundException if there is no key</returns>
        public T First<T>(string key)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                {
                    return (T)StringToTypeConverter.TryConvert(result[0], typeof(T));
                }
                return default(T);
            }
            throw new KeyNotFoundException("Parameter not found: " + key);
        }

        /// <summary>
        /// First value, or Default value if no value or key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>First value, or Default value if no value or key</returns>
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
        /// Getting the first value for the specified key, or defaults, with an attempt to convert to the specified type
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="key">Key</param>
        /// <returns>The first value, or default (T) if there are no values or a key</returns>
        public T FirstOrDefault<T>(string key)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                {
                    return (T)StringToTypeConverter.TryConvert(result[0], typeof(T));
                }
            }
            return default(T);
        }

        /// <summary>
        /// Getting the first value for the specified key, or defaults, with an attempt to convert to the specified type
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>First value, or Default value if no value or key</returns>
        public T FirstOrDefault<T>(string key, T defaultValue)
        {
            IList<string> result;
            if (_keyValues.TryGetValue(GetKey(key), out result))
            {
                if (result.Count > 0)
                {
                    return (T)StringToTypeConverter.TryConvert(result[0], typeof(T));
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Check for the presence of a key and a non-empty list of values associated with it
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>true - if a key exists and there is at least one value</returns>
        public bool Contains(string key)
        {
            key = GetKey(key);
            return _keyValues.ContainsKey(key) && _keyValues[key].Count > 0;
        }

        /// <summary>
        /// Check for one of the keys
        /// </summary>
        public bool Contains(params string[] keys)
        {
            foreach (var key in keys)
                if (Contains(key)) return true;
            return false;
        }

        /// <summary>
        /// Check for the presence of a key and its associated value
        /// </summary>
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
        /// The number of values associated with the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Number of values</returns>
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

        public IConfiguration Append(string key, IEnumerable<string> values)
        {
            if (false == _freezed)
            {
                key = GetKey(key);
                if (false == _keyValues.ContainsKey(key))
                {
                    _keyValues.TryAdd(key, new List<string>());
                }
                foreach (var value in values)
                {
                    _keyValues[key].Add(value?.Trim() ?? null);
                }
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

        public void CopyTo(IConfiguration config)
        {
            foreach (var key in this.Keys)
            {
                config.Append(key, this[key]);
            }
        }

        public void MergeFrom(IConfiguration config, ConfigurationRecordExistBehavior existRecordBehavior)
        {
            foreach (var key in config.Keys)
            {
                if (this.Contains(key))
                {
                    switch (existRecordBehavior)
                    {
                        case ConfigurationRecordExistBehavior.Append:
                            this.Append(key, config[key]);
                            break;
                        case ConfigurationRecordExistBehavior.IgnoreNew:
                            continue;
                        case ConfigurationRecordExistBehavior.Overwrite:
                            this.Remove(key);
                            this.Append(key, config[key]);
                            break;
                    }
                }
                else
                {
                    this.Append(key, config[key]);
                }
            }
        }

        public T Bind<T>() => (T)Bind(typeof(T));
        public object Bind(Type type)
        {
            var mapper = TypeMapper.Create(type, true);
            var instance = TypeHelpers.CreateInitialState(type);
            mapper.TraversalMembers(member =>
            {
                int count = Count(member.Name);
                if (count > 0)
                {
                    var values = this.Items(member.Name);
                    IConfigRecordParser parser = member.Original.GetCustomAttribute<ConfigRecordParseAttribute>()?.Parser;
                    if (TypeHelpers.IsArray(member.ClrType) && member.ClrType.GetArrayRank() == 1)
                    {
                        int index = 0;
                        var itemType = member.ClrType.GetElementType();
                        if (parser == null)
                        {
                            var elements = values.SelectMany(v => SplitRange(v, itemType)).ToArray();
                            var arrayBuilder = CollectionFactory.CreateArray(itemType, elements.Length);
                            foreach (var item in elements)
                            {
                                arrayBuilder.Set(item, index);
                                index++;
                            }
                            member.Setter(instance, arrayBuilder.Complete());
                        }
                        else
                        {
                            var elements = values.Select(v => parser.Parse(v)).ToArray();
                            var arrayBuilder = CollectionFactory.CreateArray(itemType, (elements[0] as Array).Length);
                            foreach (var item in (elements[0] as Array))
                            {
                                arrayBuilder.Set(item, index);
                                index++;
                            }
                            member.Setter(instance, arrayBuilder.Complete());
                        }
                    }
                    else if (TypeHelpers.IsEnumerable(member.ClrType) && member.ClrType != typeof(string))
                    {
                        var itemType = member.ClrType.GenericTypeArguments.First();
                        var collectionBuilder = CollectionFactory.Create(itemType);
                        if (parser == null)
                        {
                            var elements = values.SelectMany(v => SplitRange(v, itemType)).ToArray();
                            foreach (var item in elements)
                            {
                                collectionBuilder.Append(item);
                            }
                        }
                        else
                        {
                            var elements = values.Select(v => parser.Parse(v)).ToArray();
                            foreach (var item in elements)
                            {
                                collectionBuilder.Append(item);
                            }
                        }
                        member.Setter(instance, collectionBuilder.Complete());
                    }
                    else
                    {
                        var single = values.First();
                        if (parser != null)
                        {
                            member.Setter(instance, parser.Parse(single));
                        }
                        else
                        {
                            if (TypeHelpers.IsEnum(member.ClrType))
                            {
                                var value = Enum.Parse(member.ClrType, single);
                                member.Setter(instance, value);
                            }
                            else if (TypeHelpers.IsUri(member.ClrType))
                            {
                                var uri = new Uri(single);
                                member.Setter(instance, uri);
                            }
                            else if (TypeHelpers.IsIpEndPoint(member.ClrType))
                            {
                                var ep = ZeroLevel.Network.NetUtils.CreateIPEndPoint(single);
                                member.Setter(instance, ep);
                            }
                            else if (member.ClrType == typeof(IPAddress))
                            {
                                var ip = IPAddress.Parse(single);
                                member.Setter(instance, ip);
                            }
                            else
                            {
                                var itemType = member.ClrType;
                                member.Setter(instance, StringToTypeConverter.TryConvert(single, itemType));
                            }
                        }
                    }
                }
            });
            return instance;
        }


        private static IEnumerable<object> SplitRange(string line, Type elementType)
        {
            if (string.IsNullOrWhiteSpace(line))
                yield return StringToTypeConverter.TryConvert(line, elementType);
            foreach (var part in line.Split(','))
            {
                if (TypeHelpers.IsNumericType(elementType) && part.IndexOf('-') >= 0)
                {
                    var lr = part.Split('-');
                    if (lr.Length == 2)
                    {
                        // not use parser with range
                        long left = (long)Convert.ChangeType(StringToTypeConverter.TryConvert(lr[0], elementType), typeof(long));
                        long right = (long)Convert.ChangeType(StringToTypeConverter.TryConvert(lr[1], elementType), typeof(long));
                        for (; left <= right; left++)
                        {
                            yield return Convert.ChangeType(left, elementType);
                        }
                    }
                    else
                    {
                        // incorrect string
                        yield break;
                    }
                }
                else
                {
                    yield return StringToTypeConverter.TryConvert(part, elementType);
                }
            }
        }
    }
}
