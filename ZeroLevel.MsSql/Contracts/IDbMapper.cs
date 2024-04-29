using System;
using System.Data;
using System.Data.Common;

namespace ZeroLevel.MsSql
{
    public interface IDbMapper
    {
        IDbField this[string name] { get; }
        IDbField IdentityField { get; }
        Type EntityType { get; }
        object Id(object entity);
        void TraversalFields(Action<IDbField> callback);
        void TraversalFields(Func<IDbField, bool> callback);
        void SetTypeConverter(Func<IDbField, object, object> converter);
        bool Exists(string name);

        #region Serialization
        object Deserialize(DataRow row);
        object Deserialize(DbDataReader reader);
        #endregion
    }

    public interface IDbMapper<T> : IDbMapper
    {
        new T Deserialize(DataRow row);
        new T Deserialize(DbDataReader reader);
    }
}
