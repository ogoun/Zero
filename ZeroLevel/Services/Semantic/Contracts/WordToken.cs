using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.Semantic
{
    [Serializable]
    [DataContract]
    public class WordToken
    {
        [DataMember]
        public readonly string Word;
        [DataMember]
        public readonly int Position;

        public WordToken(string word, int position)
        {
            Word = word;
            Position = position;
        }
    }
}
