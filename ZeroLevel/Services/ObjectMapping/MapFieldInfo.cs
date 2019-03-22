using System;
using System.Reflection;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Services.ObjectMapping
{
    public class MapMemberInfo: 
        IMemberInfo
    {
        #region Properties
        public bool IsField { get; set; }
        public string Name { get; set; }
        public Type ClrType { get; set; }
        public Action<object, object> Setter { get; set; }
        public Func<object, object> Getter { get; set; }
        #endregion

        #region Ctor
        public MapMemberInfo(Action<object, object> setter, Func<object, object> getter)
        {
            Getter = getter;
            Setter = setter;
        }
        #endregion

        #region Factory
        public static IMemberInfo FromMember(MemberInfo memberInfo)
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
        #endregion

        #region Helpers
        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }

        private static MapMemberInfo FromField(FieldInfo fieldInfo)
        {
            var field = new MapMemberInfo(fieldInfo.BuildSetter(), fieldInfo.BuildGetter())
            {
                Name = fieldInfo.Name
            };
            field.IsField = true;
            var type = fieldInfo.FieldType;
            field.ClrType = type;
            return field;
        }

        private static IMemberInfo FromProperty(PropertyInfo propertyInfo)
        {
            var field = new MapMemberInfo(propertyInfo.BuildSetter(), propertyInfo.BuildGetter())
            {
                Name = propertyInfo.Name
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
            return null;
        }
        #endregion
    }
}
