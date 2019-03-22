using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.Reflection
{
    /// <summary>
    /// Набор методов для работы с типами объектов
    /// </summary>
    public static class TypeHelpers
    {
        /// <summary>
        /// True если массив
        /// </summary>
        public static bool IsArray(Type type)
        {
            return type.Return(t => t.IsArray, false);
        }
        /// <summary>
        /// True если структура
        /// </summary>
        public static bool IsStruct(Type type)
        {
            return type.Return(t => t.IsValueType && !IsSimpleType(t), false);
        }
        /// <summary>
        /// True если класс
        /// </summary>
        public static bool IsClass(Type type)
        {
            return type.Return(t => t.IsClass, false);
        }
        /// <summary>
        /// True если URI
        /// </summary>
        public static bool IsUri(Type type)
        {
            return type.Return(t => (typeof(Uri).IsAssignableFrom(t)), false);
        }
        /// <summary>
        /// True если хэшсет
        /// </summary>
        public static bool IsHashSet(Type type)
        {
            return type.Return(t => t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(HashSet<>), false);
        }

        /// <summary>
        /// True если строка
        /// </summary>
        public static bool IsString(Type type)
        {
            return type.Return(t => t == typeof(string));
        }
        /// <summary>
        /// True если базовый тип - date, decimal, string, или GUID
        /// </summary>
        public static bool IsSimpleType(Type type)
        {
            return type.Return(t =>
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    t = Nullable.GetUnderlyingType(t);
                }
                return t.IsPrimitive
                       || t == typeof(DateTime)
                       || t == typeof(decimal)
                       || t == typeof(string)
                       || t == typeof(Guid)
                       || t == typeof(TimeSpan);
            }, false);
        }
        /// <summary>
        /// True если тип данных
        /// </summary>
        public static bool IsRuntimeType(Type type)
        {
            return type.Return(t => (typeof(Type).IsAssignableFrom(t)), false);
        }
        /// <summary>
        /// True если IPEndPoint
        /// </summary>
        public static bool IsIpEndPoint(Type type)
        {
            return type.Return(t => t == typeof(IPEndPoint), false);
        }
        /// <summary>
        /// True если DataSet
        /// </summary>
        public static bool IsDataset(Type type)
        {
            return type.Return(t => t == typeof(DataSet), false);
        }
        /// <summary>
        /// True если DataTable
        /// </summary>
        public static bool IsDataTable(Type type)
        {
            return type.Return(t => t == typeof(DataTable), false);
        }
        /// <summary>
        /// True если DataRow
        /// </summary>
        public static bool IsDataRow(Type type)
        {
            return type.Return(t => t == typeof(DataRow), false);
        }

        public static bool IsList(Type type)
        {
            return type.Return(t => t.GetInterfaces().Contains(typeof(IList)), false);
        }

        public static bool IsDictionary(Type type)
        {
            return type.Return(tp =>
            {
                if (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return true;
                var genericInterfaces = tp.GetInterfaces().Where(t => t.IsGenericType);
                var baseDefinitions = genericInterfaces.Select(t => t.GetGenericTypeDefinition());
                return baseDefinitions.Any(t => t == typeof(IDictionary<,>));
            }, false);
        }

        public static bool IsEnumerable(Type type)
        {
            return type.Return(t => t.GetInterfaces().Contains(typeof(IEnumerable)), false);
        }

        public static bool IsGenericCollection(Type type)
        {
            return type.Return(tp =>
            {
                if (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    return true;
                }
                IEnumerable<Type> genericInterfaces = tp.GetInterfaces().Where(t => t.IsGenericType);
                IEnumerable<Type> baseDefinitions = genericInterfaces.Select(t => t.GetGenericTypeDefinition());
                var isCollectionType = baseDefinitions.Any(t => t == typeof(ICollection<>));
                return isCollectionType;
            }, false);
        }

        public static bool IsNullableType(Type type)
        {
            return type.Return(t => Nullable.GetUnderlyingType(t) != null ||
                 t.IsGenericType &&
                (t.GetGenericTypeDefinition() == typeof(Nullable<>)), false);
        }

        public static object CreateDefaultState(Type type)
        {
            return type.
                Return(t => t.IsValueType ? Activator.CreateInstance(type) : null, null);
        }

        public static object CreateInitialState(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (IsString(type))
            {
                return null;
            }
            if (IsArray(type))
            {
                return Activator.CreateInstance(type, new object[] { 0 });
            }
            if (IsList(type) || (null != type.GetInterface("ICollection`1") || type.Name.StartsWith("ICollection`1", StringComparison.Ordinal)))
            {
                Type[] arguments = type.GetGenericArguments();
                Type destListType = typeof(List<>).MakeGenericType(arguments[0]);
                return Activator.CreateInstance(destListType);
            }
            if (IsDictionary(type))
            {
                Type[] arguments = type.GetGenericArguments();
                Type dictType = typeof(Dictionary<,>).MakeGenericType(arguments[0], arguments[1]);
                return Activator.CreateInstance(dictType);
            }
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
                return Activator.CreateInstance(type);
            return FormatterServices.GetUninitializedObject(type);
        }
    }
}
