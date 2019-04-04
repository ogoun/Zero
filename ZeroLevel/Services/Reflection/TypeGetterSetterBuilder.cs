using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ZeroLevel.Services.Reflection
{
    public static class TypeGetterSetterBuilder
    {
        public static Func<object, object> BuildGetter(FieldInfo field)
        {
            if (field == null) return null;
            var entity = Expression.Parameter(typeof(object), "o");
            var target = field.DeclaringType.IsValueType ?
                            Expression.Unbox(entity, field.DeclaringType) :
                            Expression.Convert(entity, field.DeclaringType);
            var fieldExp = Expression.Field(target, field);
            var castToObject = Expression.Convert(fieldExp, typeof(object));
            var lambda = Expression.Lambda(castToObject, entity);
            return (Func<object, object>)lambda.Compile();
        }

        public static Func<object, object> BuildGetter(PropertyInfo property)
        {
            if (property == null) return null;
            if (property.CanRead == false) return null;
            var getterMethodInfo = property.GetGetMethod();
            var entity = Expression.Parameter(typeof(object), "o");
            var target = property.DeclaringType.IsValueType ?
                            Expression.Unbox(entity, property.DeclaringType) :
                            Expression.Convert(entity, property.DeclaringType);
            var getterCall = Expression.Call(target, getterMethodInfo);
            var castToObject = Expression.Convert(getterCall, typeof(object));
            var lambda = Expression.Lambda(castToObject, entity);
            return (Func<object, object>)lambda.Compile();
        }

        /// <summary>
        /// Creates a quick setter for a field.
        /// </summary>
        public static Action<object, object> BuildSetter(FieldInfo field)
        {
            if (field == null)
            {
                return null;
            }
            var instance = Expression.Parameter(typeof(object), "target");
            var inputValue = Expression.Parameter(typeof(object), "value");
            var target = field.DeclaringType.IsValueType ?
                            Expression.Unbox(instance, field.DeclaringType) :
                            Expression.Convert(instance, field.DeclaringType);
            var fieldExp = Expression.Field(target, field);

            var typeCode = Type.GetTypeCode(field.FieldType);
            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(TypeCode) });
            var convertExpression = Expression.Call(changeTypeMethod, inputValue, Expression.Constant(typeCode));

            var assignExp = Expression.Assign(fieldExp, field.FieldType.IsValueType ?
                Expression.Convert(convertExpression, field.FieldType) :
                Expression.TypeAs(convertExpression, field.FieldType));

            return Expression.Lambda<Action<object, object>>(assignExp, instance, inputValue).Compile();
        }

        /// <summary>
        /// Creates a quick setter for a property
        /// </summary>
        public static Action<object, object> BuildSetter(PropertyInfo property)
        {
            if (property == null || property.CanWrite == false)
            {
                return null;
            }
            var instance = Expression.Parameter(typeof(object), "target");
            var inputValue = Expression.Parameter(typeof(object), "value");
            var target = property.DeclaringType.IsValueType ?
                            Expression.Unbox(instance, property.DeclaringType) :
                            Expression.Convert(instance, property.DeclaringType);
            var method = property.GetSetMethod(true);
            var typeCode = Type.GetTypeCode(property.PropertyType);
            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(TypeCode) });
            var convertExpression = Expression.Call(changeTypeMethod, inputValue, Expression.Constant(typeCode));
            var setterCall = Expression.Call(target, method, Expression.Convert(convertExpression, property.PropertyType));
            var expr = Expression.Lambda<Action<object, object>>(setterCall, instance, inputValue);
            return expr.Compile();
        }

        /// <summary>
        /// Creates a quick setter for a field.
        /// </summary>
        public static Action<T, object> BuildSetter<T>(FieldInfo field)
        {
            if (field == null || typeof(T) != field.DeclaringType)
            {
                return null;
            }
            var targetExp = Expression.Parameter(typeof(T), "target");
            var inputValue = Expression.Parameter(typeof(object), "o");
            var fieldExp = Expression.Field(targetExp, field);
            var typeCode = Type.GetTypeCode(field.FieldType);
            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(TypeCode) });
            var convertExpression = Expression.Call(changeTypeMethod, inputValue, Expression.Constant(typeCode));
            var assignExp = Expression.Assign(fieldExp, Expression.Convert(convertExpression, field.FieldType));
            return Expression.Lambda<Action<T, object>>(assignExp, targetExp, inputValue).Compile();
        }

        /// <summary>
        /// Creates a quick setter for a property
        /// </summary>
        public static Action<T, object> BuildSetter<T>(PropertyInfo property)
        {
            if (property == null || typeof(T) != property.DeclaringType || property.CanWrite == false)
            {
                return null;
            }
            var method = property.GetSetMethod(true);
            var instance = Expression.Parameter(property.DeclaringType, "i");
            var target = property.DeclaringType.IsValueType ?
                            Expression.Unbox(instance, method.DeclaringType) :
                            Expression.Convert(instance, method.DeclaringType);
            var value = Expression.Parameter(typeof(object), "v");
            var typeCode = Type.GetTypeCode(property.PropertyType);
            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(TypeCode) });
            var convertExpression = Expression.Call(changeTypeMethod, value, Expression.Constant(typeCode));
            var setterCall = Expression.Call(target, method, Expression.Convert(convertExpression, property.PropertyType));
            var expr = Expression.Lambda<Action<T, object>>(setterCall, instance, value);
            return expr.Compile();
        }
    }
}