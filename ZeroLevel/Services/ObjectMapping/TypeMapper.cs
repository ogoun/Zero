using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Services.ObjectMapping
{
    public class TypeMapper :
        IMapper
    {
        #region Fields

        protected readonly Type _entityType;
        protected Func<IMemberInfo, object, object> typeConverter;
        protected readonly Dictionary<string, IMemberInfo> _fields = new Dictionary<string, IMemberInfo>();

        #endregion Fields

        #region Properties

        public IEnumerable<string> MemberNames
        {
            get
            {
                return _fields.Select(p => p.Key);
            }
        }

        public IEnumerable<IMemberInfo> Members
        {
            get
            {
                return _fields.Select(p => p.Value);
            }
        }

        public Type EntityType
        {
            get
            {
                return _entityType;
            }
        }

        public IMemberInfo this[string name]
        {
            get
            {
                return _fields[name];
            }
        }

        #endregion Properties

        #region Ctor

        public TypeMapper(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }
            _entityType = entityType;
            BuildMapping();
        }

        #endregion Ctor

        #region Public methods

        public void SetTypeConverter(Func<IMemberInfo, object, object> converter)
        {
            typeConverter = converter;
        }

        public void TraversalMembers(Action<IMemberInfo> callback)
        {
            foreach (var f in _fields) callback(f.Value);
        }

        public void TraversalMembers(Func<IMemberInfo, bool> callback)
        {
            foreach (var f in _fields) if (false == callback(f.Value)) return;
        }

        public bool Exists(string name)
        {
            return _fields.ContainsKey(name);
        }

        public void Set(object instance, string name, object value)
        {
            if (this._fields.ContainsKey(name))
            {
                var setter = this._fields[name].Setter;
                if (setter == null)
                {
                    throw new Exception($"{(this._fields[name].IsField ? "Field" : "Property")} '{name}' has not setter");
                }
                if (value == null)
                {
                    setter(instance, null);
                }
                else if (value.GetType() != this._fields[name].ClrType)
                {
                    setter(instance, Convert.ChangeType(value, this._fields[name].ClrType));
                }
                else
                {
                    setter(instance, value);
                }
            }
        }

        public object Get(object instance, string name)
        {
            if (this._fields.ContainsKey(name))
            {
                var getter = this._fields[name]?.Getter;
                if (getter == null)
                {
                    throw new Exception($"{(this._fields[name].IsField ? "Field" : "Property")} '{name}' has not getter");
                }
                return getter(instance);
            }
            throw new KeyNotFoundException($"Not found field {name}");
        }

        public T Get<T>(object instance, string name)
        {
            if (this._fields.ContainsKey(name))
            {
                var getter = this._fields[name]?.Getter;
                if (getter == null)
                {
                    throw new Exception($"{(this._fields[name].IsField ? "Field" : "Property")} '{name}' has not getter");
                }
                return (T)getter(instance);
            }
            throw new KeyNotFoundException($"Not found field {name}");
        }

        public object GetOrDefault(object instance, string name, object defaultValue)
        {
            if (this._fields.ContainsKey(name))
            {
                var getter = this._fields[name]?.Getter;
                if (getter == null)
                {
                    return defaultValue;
                }
                return getter(instance);
            }
            throw new KeyNotFoundException($"Not found field {name}");
        }

        public T GetOrDefault<T>(object instance, string name, T defaultValue)
        {
            if (this._fields.ContainsKey(name))
            {
                var getter = this._fields[name]?.Getter;
                if (getter == null)
                {
                    return defaultValue;
                }
                return (T)getter(instance);
            }
            throw new KeyNotFoundException($"Not found field {name}");
        }

        #endregion Public methods

        #region Helpers

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
                    foreach (var member in members)
                    {
                        if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                            continue;
                        var field = MapMemberInfo.FromMember(member);
                        _fields.Add(field.Name, field);
                    }
                });
        }

        #endregion Helpers

        private static readonly ConcurrentDictionary<Type, IMapper>
            _mappersCachee = new ConcurrentDictionary<Type, IMapper>();

        public static IMapper Create(Type type, bool from_cachee = true)
        {
            if (from_cachee)
            {
                if (_mappersCachee.ContainsKey(type) == false)
                {
                    _mappersCachee.TryAdd(type, new TypeMapper(type));
                }
                return _mappersCachee[type];
            }
            return new TypeMapper(type);
        }

        public static IMapper Create<T>(bool from_cachee = true)
        {
            return Create(typeof(T), from_cachee);
        }

        /// <summary>
        /// Create copy of object without call constructor
        /// </summary>
        public static object CopyDTO(object instance)
        {
            var type = instance.GetType();
            if (TypeHelpers.IsSimpleType(type))
            {
                return instance;
            }
            var copy = FormatterServices.GetSafeUninitializedObject(instance.GetType());
            var mapper = Create(instance.GetType(), true);
            foreach (var field in mapper.MemberNames)
            {
                mapper.Set(copy, field, mapper.Get(instance, field));
            }
            return copy;
        }

        public static bool EqualsDTO(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;
            if (right == null) return false;
            if (left.GetType() != right.GetType()) return false;
            var lt = left.GetType();
            if (TypeHelpers.IsSimpleType(lt))
            {
                return left == right;
            }
            else if (lt == typeof(string))
            {
                return string.Equals((string)left, (string)right);
            }
            var mapper = Create(lt, true);
            foreach (var field in mapper.MemberNames)
            {
                var lval = mapper.Get(left, field);
                var rval = mapper.Get(right, field);
                if (object.Equals(lval, rval) == false) return false;
            }
            return true;
        }
    }
}