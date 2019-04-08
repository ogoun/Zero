using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ZeroLevel.Services.Invokation
{
    /// <summary>
    /// Method call wrapper
    /// </summary>
    public class InvokeWrapper : IInvokeWrapper
    {
        /// <summary>
        /// Cahce
        /// </summary>
        protected readonly Dictionary<string, Invoker> _invokeCachee = new Dictionary<string, Invoker>();

        #region Static helpers

        /// <summary>
        /// Creates a compiled expression for a quick method call, returns the identifier of the expression and a delegate for the call.
        /// </summary>
        /// <param name="method">Wrapped method</param>
        /// <returns>Expression ID and Delegate Tuple</returns>
        protected static Tuple<string, Invoker> CreateCompiledExpression(MethodInfo method)
        {
            var targetArg = Expression.Parameter(typeof(object));
            var argsArg = Expression.Parameter(typeof(object[]));
            var parameters = method.GetParameters();
            Expression body = Expression.Call(
                method.IsStatic
                    ? null
                    : Expression.Convert(targetArg, method.DeclaringType), //  the type in which the method is declared
                method,
                parameters.Select((p, i) =>
                    Expression.Convert(Expression.ArrayIndex(argsArg, Expression.Constant(i)), p.ParameterType)));
            if (body.Type == typeof(void))
                body = Expression.Block(body, Expression.Constant(null));
            else if (body.Type.IsValueType)
                body = Expression.Convert(body, typeof(object));
            var identity = CreateMethodIdentity(method.Name, parameters.Select(p => p.ParameterType).ToArray());
            return new Tuple<string, Invoker>(identity.ToString(),
                Expression.Lambda<Invoker>(body, targetArg, argsArg).Compile());
        }

        /// <summary>
        /// Wraps Delegate Call
        /// </summary>
        /// <param name="handler">Wrapped delegate</param>
        /// <returns>Expression ID and Delegate Tuple</returns>
        protected static Tuple<string, Invoker> CreateCompiledExpression(Delegate handler)
        {
            return CreateCompiledExpression(handler.GetMethodInfo());
        }

        #endregion Static helpers

        #region Helpers

        /// <summary>
        /// ID uniquely identifying method at the type level (but not at the global level)
        /// </summary>
        /// <param name="name">Method name</param>
        /// <param name="argsTypes">Method Argument Types</param>
        /// <returns></returns>
        internal static string CreateMethodIdentity(string name, params Type[] argsTypes)
        {
            var identity = new StringBuilder(name);
            identity.Append("(");
            if (null != argsTypes)
            {
                for (var i = 0; i < argsTypes.Length; i++)
                {
                    identity.Append(argsTypes[i].Name);
                    if ((i + 1) < argsTypes.Length)
                        identity.Append(".");
                }
            }

            identity.Append(")");
            return identity.ToString();
        }

        #endregion Helpers

        #region Configure by Type

        public IEnumerable<string> Configure<T>()
        {
            return Configure(typeof(T));
        }

        public IEnumerable<string> Configure<T>(string methodName)
        {
            return Configure(typeof(T), methodName);
        }

        public IEnumerable<string> Configure<T>(Func<MethodInfo, bool> filter)
        {
            return Configure(typeof(T), filter);
        }

        public IEnumerable<string> Configure(Type instanceType)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> Configure(Type instanceType, string methodName)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(m => m.Name.Equals(methodName))
                ?.Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> ConfigureGeneric<T>(Type instanceType, string methodName)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(m => m.Name.Equals(methodName))
                ?.Select(method => method.MakeGenericMethod(typeof(T))).Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> ConfigureGeneric(Type instanceType, Type genericType, string methodName)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(m => m.Name.Equals(methodName))
                ?.Select(method => method.MakeGenericMethod(genericType)).Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> ConfigureGeneric<T>(Type instanceType, Func<MethodInfo, bool> filter)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(filter)
                ?.Select(method => method.MakeGenericMethod(typeof(T))).Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> ConfigureGeneric(Type instanceType, Type genericType, Func<MethodInfo, bool> filter)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(filter)
                ?.Select(method => method.MakeGenericMethod(genericType)).Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> Configure(Type instanceType, Func<MethodInfo, bool> filter)
        {
            var result = instanceType.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(filter)
                ?.Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        #endregion Configure by Type

        #region Configure by MethodInfo

        /// <summary>
        /// Cache the specified method
        /// </summary>
        /// <param name="method">Method</param>
        /// <returns>Call ID</returns>
        public string Configure(MethodInfo method)
        {
            var invoke = CreateCompiledExpression(method);
            Configure(invoke);
            return invoke.Item1;
        }

        /// <summary>
        /// Cache the specified delegate
        /// </summary>
        /// <param name="handler">Delegate</param>
        /// <returns>Call ID</returns>
        public string Configure(Delegate handler)
        {
            var invoke = CreateCompiledExpression(handler);
            Configure(invoke);
            return invoke.Item1;
        }

        public IEnumerable<string> Configure(IEnumerable<MethodInfo> list)
        {
            var result = list?.Select(CreateCompiledExpression);
            if (null != result)
            {
                Configure(result);
                return result.Select(r => r.Item1).ToList();
            }

            return Enumerable.Empty<string>();
        }

        #endregion Configure by MethodInfo

        #region Configuration

        /// <summary>
        /// Filling the cache from the list of methods with identifiers
        /// </summary>
        protected void Configure(IEnumerable<Tuple<string, Invoker>> list)
        {
            foreach (var invoke in list)
            {
                Configure(invoke);
            }
        }

        /// <summary>
        /// Adding a call to the cache
        /// </summary>
        protected void Configure(Tuple<string, Invoker> invoke)
        {
            _invokeCachee[invoke.Item1] = invoke.Item2;
        }

        #endregion Configuration

        #region Invoking

        /// <summary>
        /// Calling a static method by identifier, if there is no method in the cache, a KeyNotFoundException exception will be thrown
        /// </summary>
        /// <param name="identity">Call ID</param>
        /// <param name="args">Method Arguments</param>
        /// <returns>Execution result</returns>
        public object InvokeStatic(string identity, object[] args)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity](null, args);
            }

            throw new KeyNotFoundException(String.Format("Not found method with identity '{0}'", identity));
        }

        /// <summary>
        /// Calling a method by identifier; if there is no method in the cache, KeyNotFoundException will be thrown.
        /// </summary>
        /// <param name="target">The instance on which the method is called</param>
        /// <param name="identity">Call ID</param>
        /// <param name="args">Method Arguments</param>
        /// <returns>Execution result</returns>
        public object Invoke(object target, string identity, object[] args)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity](target, args);
            }

            throw new KeyNotFoundException($"Not found method with identity '{identity}'");
        }

        public object Invoke(object target, string identity)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity](target, null);
            }

            throw new KeyNotFoundException($"Not found method with identity '{identity}'");
        }

        /// <summary>
        /// Execution of a static cached method
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="args">Method Arguments</param>
        /// /// <returns>Execution result</returns>
        public object Invoke(string methodName, object[] args)
        {
            return InvokeStatic(CreateMethodIdentity(methodName, args.Select(a => a.GetType()).ToArray()), args);
        }

        #endregion Invoking

        #region Helpers

        /// <summary>
        /// Request call id for method
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="argsTypes">Method argument type list</param>
        /// <returns>Call ID</returns>
        public string GetInvokerIdentity(string methodName, params Type[] argsTypes)
        {
            return CreateMethodIdentity(methodName, argsTypes);
        }

        /// <summary>
        /// Request for delegate to wrap method
        /// </summary>
        /// <param name="identity">Call ID</param>
        /// <returns>Delegate</returns>
        public Invoker GetInvoker(string identity)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity];
            }

            return null;
        }

        /// <summary>
        /// Request for delegate to wrap method
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="argsTypes">Method argument type list</param>
        /// <returns>Delegate</returns>
        public Invoker GetInvoker(string methodName, params Type[] argsTypes)
        {
            return GetInvoker(CreateMethodIdentity(methodName, argsTypes));
        }

        #endregion Helpers

        #region Factories

        public static IInvokeWrapper Create()
        {
            return new InvokeWrapper();
        }

        #endregion Factories
    }
}