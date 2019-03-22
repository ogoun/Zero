using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZeroLevel.Services.Invokation
{
    public interface IInvokeWrapper
    {
        IEnumerable<string> Configure<T>();
        IEnumerable<string> Configure<T>(string methodName);
        IEnumerable<string> Configure<T>(Func<MethodInfo, bool> filter);
        IEnumerable<string> Configure(Type instanceType);
        IEnumerable<string> Configure(Type instanceType, string methodName);
        IEnumerable<string> Configure(Type instanceType, Func<MethodInfo, bool> filter);

        IEnumerable<string> ConfigureGeneric<T>(Type instanceType, string methodName);
        IEnumerable<string> ConfigureGeneric<T>(Type instanceType, Func<MethodInfo, bool> filter);
        IEnumerable<string> ConfigureGeneric(Type instanceType, Type genericType, string methodName);
        IEnumerable<string> ConfigureGeneric(Type instanceType, Type genericType, Func<MethodInfo, bool> filter);
        string Configure(MethodInfo method);
        string Configure(Delegate handler);
        IEnumerable<string> Configure(IEnumerable<MethodInfo> list);
        object InvokeStatic(string identity, object[] args);
        object Invoke(object target, string identity, object[] args);
        object Invoke(object target, string identity);
        object Invoke(string methodName, object[] args);
        string GetInvokerIdentity(string methodName, params Type[] argsTypes);
        Invoker GetInvoker(string identity);
        Invoker GetInvoker(string methodName, params Type[] argsTypes);
    }
}
