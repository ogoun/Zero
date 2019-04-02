using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Models
{
    [DataContract]
    [Serializable]
    public abstract class BaseModel
    {
        #region Equal
        public bool Equals(BaseModel other)
        {
            if (this == null)
                throw new NullReferenceException();
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (this == null)
                throw new NullReferenceException();

            return Equals(obj as BaseModel);
        }

        public static bool operator ==(BaseModel first, BaseModel second) => Equals(first, second);
        public static bool operator !=(BaseModel first, BaseModel second) => !Equals(first, second);
        #endregion

        public abstract override int GetHashCode();
        public abstract object Clone();
    }
}
