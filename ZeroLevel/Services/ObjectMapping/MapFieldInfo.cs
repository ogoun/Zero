using System;
using System.Reflection;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Services.ObjectMapping
{
    public class MapMemberInfo :
        IMemberInfo
    {
        #region Properties

        public bool IsField { get; set; }
        public string Name { get; set; }
        public Type ClrType { get; set; }
        public Action<object, object> Setter { get; private set; }
        public Func<object, object> Getter { get; private set; }
        public MemberInfo Original { get; private set; }

        #endregion Properties

        #region Ctor

        public MapMemberInfo(Action<object, object> setter, Func<object, object> getter)
        {
            Getter = getter;
            Setter = setter;
        }

        #endregion Ctor

        #region Factory

        public static IMemberInfo FromMember(MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return FromField((memberInfo as FieldInfo)!);

                case MemberTypes.Property:
                    return FromProperty((memberInfo as PropertyInfo)!);
            }
            return null!;
        }

        #endregion Factory

        #region Helpers

        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null!) return true; // Nullable<T>
            return false; // value-type
        }

        private static MapMemberInfo FromField(FieldInfo fieldInfo)
        {
            if (fieldInfo == null) return null!;
            var field = new MapMemberInfo(TypeGetterSetterBuilder.BuildSetter(fieldInfo), TypeGetterSetterBuilder.BuildGetter(fieldInfo))
            {
                Name = fieldInfo.Name,
                Original = fieldInfo
            };
            field.IsField = true;
            var type = fieldInfo.FieldType;
            field.ClrType = type;
            return field;
        }

        private static IMemberInfo FromProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) return null!;
            var field = new MapMemberInfo(TypeGetterSetterBuilder.BuildSetter(propertyInfo), TypeGetterSetterBuilder.BuildGetter(propertyInfo))
            {
                Name = propertyInfo.Name,
                Original = propertyInfo
            };
            field.IsField = false;
            var type = propertyInfo.PropertyType;
            field.ClrType = type;
            return field;
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null!;
        }

        #endregion Helpers
    }
}