using System;
using System.Runtime.Serialization;

namespace ZeroLevel.MsSql
{
    [DataContract]
    [Serializable]
    public abstract class BaseEntity : IEntity
    {
        #region Properties
        [DataMember]
        [DbMember(false, true, false)]
        public Guid Id
        {
            get;
            set;
        }
        #endregion

        #region Ctors
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }
        protected BaseEntity(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Entity id must not be empty");
            Id = id;
        }
        protected BaseEntity(BaseEntity other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            Id = other.Id;
        }
        #endregion

        public abstract object Clone();

        #region Equal
        public bool Equals(BaseEntity other)
        {
            if (this == null) // и так бывает
                throw new NullReferenceException();
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (this == null)
                throw new NullReferenceException();

            return Equals(obj as BaseEntity);
        }

        public static bool operator ==(BaseEntity first, BaseEntity second) => Equals(first, second);
        public static bool operator !=(BaseEntity first, BaseEntity second) => !Equals(first, second);
        #endregion

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
