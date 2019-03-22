using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ZeroLevel.Services.Reflection
{
    public class TypeFastAccessMethodBuilderImpl : ITypeFastAccessMethodBuilder
    {
        public Func<object, object> BuildGetter(PropertyInfo property)
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

        public Func<object, object> BuildGetter(FieldInfo field)
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
        /// <summary>
        /// Создает быстрый сеттер для свойства
        /// </summary>
        public Action<object, object> BuildSetter(PropertyInfo property)
        {
            if (property == null) return null;
            if (property.CanWrite == false) return null;
            var method = property.GetSetMethod(true);
            var obj = Expression.Parameter(typeof(object), "o");
            var value = Expression.Parameter(typeof(object));
            var target = property.DeclaringType.IsValueType ?
                            Expression.Unbox(obj, method.DeclaringType) :
                            Expression.Convert(obj, method.DeclaringType);
            var expr = Expression.
                Lambda<Action<object, object>>(Expression.Call(target, method,
                        Expression.Convert(value, method.GetParameters()[0].ParameterType)),
                        obj, value);
            return expr.Compile();
        }
        /// <summary>
        /// Создает быстрый сеттер для поля
        /// </summary>
        public Action<object, object> BuildSetter(FieldInfo field)
        {
            if (field == null) return null;
            var targetExp = Expression.Parameter(field.DeclaringType, "target");
            var valueExp = Expression.Parameter(typeof(object), "value");

            // Expression.Property can be used here as well
            var obj = Expression.Parameter(typeof(object), "o");
            var target = field.DeclaringType.IsValueType ?
                            Expression.Unbox(obj, field.DeclaringType) :
                            Expression.Convert(obj, field.DeclaringType);

            var fieldExp = Expression.Field(target, field);
            var assignExp = Expression.Assign(fieldExp, Expression.Convert(valueExp, field.FieldType));

            return Expression.Lambda<Action<object, object>>
                (assignExp, obj, valueExp).Compile();
        }
    }
}
