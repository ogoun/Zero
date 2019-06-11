using System;
using System.Runtime.Serialization;

namespace ZeroLevel.SqlServer
{
    [DataContract]
    [Serializable]
    public abstract class BaseVersionedEntity : BaseEntity, IVersionedEntity
    {
        #region Properties
        [DataMember]
        [DbMember(false)]
        public long Version
        {
            get;
            internal set;
        }
        #endregion

        #region Ctors
        protected BaseVersionedEntity()
            : base()
        {
        }
        // Конструктор protected BaseVersionedEntity(Guid id) исключен, т.к. без версии нет смысла создавать обхект с известным ID

        protected BaseVersionedEntity(Guid id, long version)
            : base(id)
        {
            Version = version;
        }
        protected BaseVersionedEntity(BaseVersionedEntity other)
            : base(other)
        {
            Version = other.Version;
        }
        #endregion

        public bool Equals(BaseVersionedEntity other)
        {
            if (base.Equals(other) == false) return false;
            return Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BaseVersionedEntity);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
