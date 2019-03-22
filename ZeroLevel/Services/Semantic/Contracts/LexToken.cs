using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.Semantic
{
    [Serializable]
    [DataContract]
    public class LexToken
    {
        [DataMember]
        public readonly string Word;
        [DataMember]
        public readonly string Token;
        [DataMember]
        public readonly int Position;

        public LexToken(string word, string token, int position)
        {
            Word = word;
            Token = token;
            Position = position;
        }

        public override bool Equals(object obj)
        {
            if (this == null)
                throw new NullReferenceException();
            return this.Equals(obj as LexToken);
        }

        public bool Equals(LexToken other)
        {
            if ((object)this == (object)other)
                return true;
            if (this == null) // и так бывает
                throw new NullReferenceException();
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            if (false == string.Equals(other.Word, this.Word, StringComparison.Ordinal)) return false;
            if (false == string.Equals(other.Token, this.Token, StringComparison.Ordinal)) return false;
            return this.Position == other.Position;
        }

        public override int GetHashCode()
        {
            return this.Word.GetHashCode() ^ this.Token.GetHashCode() & this.Position.GetHashCode();
        }
    }
}
