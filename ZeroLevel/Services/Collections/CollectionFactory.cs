using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Invokation;

namespace ZeroLevel.Services.Collections
{
    public interface ICollectionBuilder
    {
        void Append(object item);
        IEnumerable Complete();
    }
    public interface IArrayBuilder
    {
        void Set(object item, int index);
        object Complete();
    }
    public class CollectionFactory 
    {
        public static ICollectionBuilder Create<T>() => Create(typeof(T));

        public static ICollectionBuilder Create(Type type)
        {
            return new IEnumerableBuilder(type);
        }

        public static IArrayBuilder CreateArray<T>(int count) => CreateArray(typeof(T), count);

        public static IArrayBuilder CreateArray(Type type, int count)
        {
            return new ArrayBuilder(type, count);
        }

        private class IEnumerableBuilder
            : ICollectionBuilder
        {
            private readonly IInvokeWrapper _wrapper;

            private readonly Invoker _insert;
            private readonly object _instance;

            public IEnumerableBuilder(Type entityType)
            {
                _wrapper = InvokeWrapper.Create();
                var genericType = typeof(List<>);
                var instanceType = genericType.MakeGenericType(new Type[] { entityType });
                _instance = Activator.CreateInstance(instanceType);

                var insert_key = _wrapper.Configure(instanceType, "Add").Single();
                _insert = _wrapper.GetInvoker(insert_key);
            }

            public void Append(object item)
            {
                _insert.Invoke(_instance, new object[] { item });
            }

            public IEnumerable Complete()
            {
                return (IEnumerable)_instance;
            }
        }

        private class ArrayBuilder
            : IArrayBuilder
        {
            private readonly IInvokeWrapper _wrapper;

            private readonly Invoker _insert;
            private readonly object _instance;

            public ArrayBuilder(Type entityType, int count)
            {
                _instance = Array.CreateInstance(entityType, count);
            }

            public void Set(object item, int index)
            {
                ((Array)_instance).SetValue(item, index);
            }

            public object Complete()
            {
                return _instance;
            }
        }
    }
}
