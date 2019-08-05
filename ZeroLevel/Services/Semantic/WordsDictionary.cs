using System.Collections.Concurrent;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Semantic
{
    public class WordsDictionary
        : IBinarySerializable
    {
        private ConcurrentDictionary<string, LanguageDictionary> _dicts = new ConcurrentDictionary<string, LanguageDictionary>();
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public LanguageDictionary this[string lang]
        {
            get
            {
                if (_dicts.ContainsKey(lang) == false)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        if (_dicts.ContainsKey(lang) == false)
                        {
                            _dicts[lang] = new LanguageDictionary();
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
                return _dicts[lang];
            }
        }
        public void ToggleReverseIndex(bool enabled)
        {
            foreach (var pair in _dicts)
            {
                pair.Value.ToggleReverseIndex(enabled);
            }
        }
        public void Deserialize(IBinaryReader reader)
        {
            int count = reader.ReadInt32();
            this._dicts = new ConcurrentDictionary<string, LanguageDictionary>();
            string key;
            for (int i = 0; i < count; i++)
            {
                key = reader.ReadString();
                this._dicts.TryAdd(key, reader.Read<LanguageDictionary>());
            }
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(_dicts.Count);
            foreach (var pair in _dicts)
            {
                writer.WriteString(pair.Key);
                writer.Write<LanguageDictionary>(pair.Value);
            }
        }
    }
}
