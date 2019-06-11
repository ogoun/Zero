using System;
using System.Data;
using System.Reflection;
using ZeroLevel.Services.ObjectMapping;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.SqlServer
{
    public class DbField : MapMemberInfo, IDbField
    {
        public bool AutoIncrement { get; internal set; }
        public bool IsIdentity { get; internal set; }
        public bool IsIndexed { get; internal set; }
        public bool AllowNull { get; internal set; }
        public long Size { get; internal set; }
        public DbType DbType { get; internal set; }

        private DbField(Action<object, object> setter, Func<object, object> getter)
            :base(setter, getter)
        {
        }

        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }

        public static DbField FromField(FieldInfo fieldInfo)
        {
            var meta = ((DbMemberAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DbMemberAttribute)));
            var index = ((DbIndexAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DbIndexAttribute)));
            var field = new DbField(TypeGetterSetterBuilder.BuildSetter(fieldInfo), TypeGetterSetterBuilder.BuildGetter(fieldInfo))
            {
                Name = fieldInfo.Name,
                IsIdentity = meta?.IsIdentity ?? false,
                AllowNull = meta?.AllowNull ?? true,
                AutoIncrement = meta?.AutoIncrement ?? false,
                Size = meta?.Size ?? -1,
                IsIndexed = index != null
            };
            field.IsField = true;
            var type = fieldInfo.FieldType;
            field.ClrType = type;
            field.DbType = type.ToDbType();
            return field;
        }

        public static DbField FromProperty(PropertyInfo propertyInfo)
        {
            var meta = ((DbMemberAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(DbMemberAttribute)));
            var index = ((DbIndexAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(DbIndexAttribute)));
            var field = new DbField(TypeGetterSetterBuilder.BuildSetter(propertyInfo), TypeGetterSetterBuilder.BuildGetter(propertyInfo))
            {
                Name = propertyInfo.Name,
                IsIdentity = meta?.IsIdentity ?? false,
                AllowNull = meta?.AllowNull ?? true,
                AutoIncrement = meta?.AutoIncrement ?? false,
                Size = meta?.Size ?? -1,
                IsIndexed = index != null
            };
            field.IsField = false;
            var type = propertyInfo.PropertyType;
            field.ClrType = type;
            field.DbType = type.ToDbType();
            return field;
        }

        public new static DbField FromMember(MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return FromField(memberInfo as FieldInfo);
                case MemberTypes.Property:
                    return FromProperty(memberInfo as PropertyInfo);
            }
            return null;
        }

        public void SetValue(object instance, object dbvalue, Func<DbField, object, object> converter = null)
        {
            var value = (null == dbvalue || DBNull.Value == dbvalue) ? null : dbvalue;
            if (null != converter)
            {
                value = converter(this, value);
            }
            if (null == value && false == IsNullable(ClrType))
                Setter(instance, TypeExtensions.GetDefault(ClrType));
            else
                Setter(instance, value);
        }

    }
}
