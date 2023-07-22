using System;
using System.Linq;
using System.Reflection;
using ZeroLevel.Services.Invokation;
using ZeroLevel.Services.ObjectMapping;
using ZeroLevel.Services.Serialization;

namespace TestApp
{
    internal static class Program
    {
        private class Wrapper
        {
            public string ReadId;
            public string WriteId;
            public IInvokeWrapper Invoker;

            public T Read<T>(IBinaryReader reader)
            {
                return (T)Invoker.Invoke(reader, ReadId);
            }

            public object ReadObject(IBinaryReader reader)
            {
                return Invoker.Invoke(reader, ReadId);
            }

            public void Write<T>(IBinaryWriter writer, T value)
            {
                Invoker.Invoke(writer, WriteId, new object[] { value });
            }

            public void WriteObject(IBinaryWriter writer, object value)
            {
                Invoker.Invoke(writer, WriteId, new object[] { value });
            }
        }

        private static Func<MethodInfo, bool> CreateArrayPredicate<T>()
        {
            var typeArg = typeof(T).GetElementType();
            return mi => mi.Name.Equals("WriteArray", StringComparison.Ordinal) &&
                mi.GetParameters().First().ParameterType.GetElementType().IsAssignableFrom(typeArg);
        }

        private static Func<MethodInfo, bool> CreateCollectionPredicate<T>()
        {
            var typeArg = typeof(T).GetGenericArguments().First();
            return mi => mi.Name.Equals("WriteCollection", StringComparison.Ordinal) &&
            mi.GetParameters().First().ParameterType.GetGenericArguments().First().IsAssignableFrom(typeArg);
        }

        private static void Main(string[] args)
        {
            var wrapper = new Wrapper { Invoker = InvokeWrapper.Create() };
            var ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTimeArray").First();
            var WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<DateTime?[]>()).First();

            Console.Write(WriteId);
        }
    }
}