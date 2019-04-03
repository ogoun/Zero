using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ZeroLevel
{
    public static class TypeExtensions
    {
        public static bool IsAsyncMethod(this Type classType, string methodName)
        {
            // Obtain the method with the specified name.
            MethodInfo method = classType.GetMethod(methodName);
            return IsAsyncMethod(method);
        }

        public static bool IsAsyncMethod(this MethodInfo method)
        {
            Type attType = typeof(AsyncStateMachineAttribute);
            // Obtain the custom attribute for the method.
            // The value returned contains the StateMachineType property.
            // Null is returned if the attribute isn't present for the method.
            var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);
            return (attrib != null);
        }

        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static object GetPropValue(this object src, string propName)
        {
            return src?.GetType()?.GetProperty(propName)?.GetValue(src, null);
        }

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }
    }
}