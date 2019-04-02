using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Config
{
    /// <summary>
    /// Named configuration sections array
    /// </summary>
    internal sealed class BaseConfigurationSet :
        IConfigurationSet
    {
        #region Private members
        /// <summary>
        /// Sections
        /// </summary>
        private readonly ConcurrentDictionary<string, IConfiguration> _sections = new ConcurrentDictionary<string, IConfiguration>();

        private static string GetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            return key.Trim().ToLower(CultureInfo.InvariantCulture);
        }
        #endregion

        #region Properties
        public IConfiguration Default
        {
            get { return _sections[Configuration.DEFAULT_SECTION_NAME]; }
        }

        public IConfiguration this[string sectionName]
        {
            get
            {
                return CreateSection(sectionName);
            }
        }

        public IEnumerable<string> SectionNames
        {
            get { return _sections.Keys; }
        }

        public IEnumerable<IConfiguration> Sections
        {
            get { return _sections.Values; }
        }

        public bool SectionsFreezed
        {
            get
            {
                return _sectionsFreezed;
            }
        }

        #endregion

        #region Methods
        public BaseConfigurationSet()
        {
            CreateSection(Configuration.DEFAULT_SECTION_NAME);
        }

        public BaseConfigurationSet(IConfiguration defaultConfiguration)
        {
            _sections.TryAdd(Configuration.DEFAULT_SECTION_NAME, defaultConfiguration);
        }

        public IConfiguration CreateSection(string sectionName)
        {
            var key = GetKey(sectionName);
            IConfiguration exists;
            if (_sections.TryGetValue(key, out exists))
            {
                return exists;
            }
            else if (false == _sectionsFreezed)
            {
                _sections.TryAdd(key, new BaseConfiguration());
                return _sections[key];
            }
            throw new Exception("Sections change freezed");
        }

        public IConfiguration GetSection(string sectionName)
        {
            var key = GetKey(sectionName);
            IConfiguration exists;
            if (_sections.TryGetValue(key, out exists))
            {
                return exists;
            }
            throw new KeyNotFoundException("Section not found: " + sectionName);
        }

        public bool ContainsSection(string sectionName)
        {
            var key = GetKey(sectionName);
            return _sections.ContainsKey(key);
        }

        public bool RemoveSection(string sectionName)
        {
            if (false == _sectionsFreezed)
            {
                var key = GetKey(sectionName);
                if (_sections.ContainsKey(key))
                {
                    IConfiguration removed;
                    return _sections.TryRemove(key, out removed);
                }
            }
            return false;
        }
        #endregion

        #region IEquatable
        public bool Equals(IConfigurationSet other)
        {
            if (other == null) return false;
            return this.SectionNames.NoOrderingEquals(other.SectionNames) &&
                this.Sections.NoOrderingEquals(other.Sections);
        }
        #endregion

        #region Freezing
        private readonly object _freezeLock = new object();

        public bool FreezeConfiguration(bool permanent = false)
        {
            bool result = false;
            lock (_freezeLock)
            {
                foreach (var s in _sections)
                {
                    result |= s.Value.Freeze(permanent);
                }
            }
            return result || FreezeSections(permanent);
        }

        public bool UnfreezeConfiguration()
        {
            bool result = false;
            lock (_freezeLock)
            {
                foreach (var s in _sections)
                {
                    result |= s.Value.Unfreeze();
                }
            }
            return result || UnfreezeSections();
        }

        private bool _sectionsFreezed = false;
        private bool _permanentSectionsFreezed = false;

        public bool FreezeSections(bool permanent = false)
        {
            lock (_freezeLock)
            {
                if (false == _sectionsFreezed)
                {
                    _sectionsFreezed = true;
                    _permanentSectionsFreezed = permanent;
                    return true;
                }
                else if (_permanentSectionsFreezed == false && permanent)
                {
                    _permanentSectionsFreezed = true;
                    return true;
                }
                return false;
            }
        }

        public bool UnfreezeSections()
        {
            lock (_freezeLock)
            {
                if (_sectionsFreezed && _permanentSectionsFreezed == false)
                {
                    _sectionsFreezed = false;
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region Binary Serializable
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(this._sectionsFreezed);
            writer.WriteBoolean(this._permanentSectionsFreezed);
            writer.WriteInt32(_sections.Count);
            foreach (var s in _sections)
            {
                writer.WriteString(s.Key);
                writer.Write<IConfiguration>(s.Value);
            }
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._sectionsFreezed = reader.ReadBoolean();
            this._permanentSectionsFreezed = reader.ReadBoolean();
            var count = reader.ReadInt32();
            _sections.Clear();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                _sections.TryAdd(key, reader.Read<BaseConfiguration>());
            }
        }
        #endregion
    }
}
