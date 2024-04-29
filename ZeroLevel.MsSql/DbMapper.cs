using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace ZeroLevel.MsSql
{
    public class DbMapper: IDbMapper
    {
        protected readonly Dictionary<string, DbField> _fields = new Dictionary<string, DbField>();
        private string _identityFieldName;
        private readonly Type _entityType;
        /// <summary>
        /// В случае задания в true, все поля класса считаются данными модели, в т.ч. не отвеченные аттрибутом DbMember
        /// </summary>
        private readonly bool _marked_only;
        protected Func<DbField, object, object> typeConverter;

        public void SetTypeConverter(Func<IDbField, object, object> converter)
        {
            typeConverter = converter;
        }

        public IDbField IdentityField
        {
            get
            {
                if (false == string.IsNullOrWhiteSpace(_identityFieldName))
                {
                    return _fields[_identityFieldName];
                }
                return null;
            }
        }

        public Type EntityType
        {
            get
            {
                return _entityType;
            }
        }

        public IDbField this[string name]
        {
            get
            {
                return _fields[name];
            }
        }

        public object Id(object entity)
        {
            return IdentityField?.Getter(entity);
        }

        internal DbMapper(Type entityType, bool mapOnlyMarkedMembers)
        {
            _marked_only = mapOnlyMarkedMembers;
            _entityType = entityType;
            BuildMapping();
        }

        private void BuildMapping()
        {
            _entityType.GetMembers(
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy |
                BindingFlags.GetField |
                BindingFlags.GetProperty |
                BindingFlags.Instance).
                Do(members =>
                {
                    IEnumerable<MemberInfo> memberList;
                    if (_marked_only)
                    {
                        memberList = members.Where(m => null != Attribute.GetCustomAttribute(m, typeof(DbMemberAttribute)));
                    }
                    else
                    {
                        memberList = members;
                    }
                    foreach (var member in memberList)
                    {
                        if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                            continue;
                        var field = DbField.FromMember(member);
                        if (field.IsIdentity)
                        {
                            _identityFieldName = member.Name;
                        }
                        _fields.Add(field.Name, field);
                    }
                    if (true == string.IsNullOrWhiteSpace(_identityFieldName))
                    {
                        _identityFieldName = _fields.Keys.FirstOrDefault(f => f.Equals("id", StringComparison.OrdinalIgnoreCase));
                        if (true == string.IsNullOrWhiteSpace(_identityFieldName))
                        {
                            _identityFieldName = _fields.Keys.FirstOrDefault(f =>
                                f.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                f.IndexOf(_entityType.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                    }
                    if (false == string.IsNullOrWhiteSpace(_identityFieldName))
                    {
                        _fields[_identityFieldName].IsIdentity = true;
                        _fields[_identityFieldName].AllowNull = false;
                    }
                });
        }

        public void TraversalFields(Action<IDbField> callback)
        {
            foreach (var f in _fields) callback(f.Value);
        }

        public void TraversalFields(Func<IDbField, bool> callback)
        {
            foreach (var f in _fields) if (false == callback(f.Value)) return;
        }

        public bool Exists(string name)
        {
            return _fields.ContainsKey(name);
        }

        #region Serialization
        public object Deserialize(DataRow row)
        {
            if (null == row) throw new ArgumentNullException(nameof(row));
            var result = Activator.CreateInstance(_entityType);
            foreach (var field in _fields)
            {
                var value = (null == row[field.Key] || DBNull.Value == row[field.Key]) ? null : row[field.Key];
                if (null != typeConverter)
                {
                    field.Value.Setter(result, typeConverter(field.Value, value));
                }
                else
                {
                    field.Value.Setter(result, value);
                }
            }
            return result;
        }

        public object Deserialize(DbDataReader reader)
        {
            if (null == reader) throw new ArgumentNullException(nameof(reader));
            var result = Activator.CreateInstance(_entityType);
            foreach (var field in _fields)
            {
                var value = (null == reader[field.Key] || DBNull.Value == reader[field.Key]) ? null : reader[field.Key];
                if (null != typeConverter)
                {
                    field.Value.Setter(result, typeConverter(field.Value, value));
                }
                else
                {
                    field.Value.Setter(result, value);
                }
            }
            return result;
        }
        #endregion
    }
}
