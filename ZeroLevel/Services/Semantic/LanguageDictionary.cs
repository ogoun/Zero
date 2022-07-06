using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Semantic
{
    public class LanguageDictionary
        : IBinarySerializable
    {
        private Trie _words = new Trie();
        public uint this[string word] => _words.Key(word) ?? 0;
        public string Word(uint key) => _words.Word(key);

        public IEnumerable<uint> Keys => _words.Keys;

        public void Append(string word)
        {
            _words.Append(word.Normalize());
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._words = reader.Read<Trie>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.Write(this._words);
        }

        public void ToggleReverseIndex(bool enabled)
        {
            _words.ToggleReverseIndex(enabled);
        }
    }
}
