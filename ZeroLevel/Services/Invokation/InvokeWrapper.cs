using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ZeroLevel.Services.Invokation
{
    /// <summary>
    /// Обертка для вызова методов
    /// </summary>
    public class InvokeWrapper : IInvokeWrapper
    {
        /// <summary>
        /// Кэш делегатов
        /// </summary>
        protected readonly Dictionary<string, Invoker> _invokeCachee = new Dictionary<string, Invoker>();

        #region Static helpers

        /// <summary>
        /// Создает скомпилированное выражение для быстрого вызова метода, возвращает идентификатор выражения и делегат для вызова
        /// </summary>
        /// <param name="method">Оборачиваемый метод</param>
        /// <returns>Кортеж с идентификатором выражения и делегатом</returns>
        protected static Tuple<string, Invoker> CreateCompiledExpression(MethodInfo method)
        {
            var targetArg = Expression.Parameter(typeof(object)); //  Цель на которой происходит вызов
            var argsArg = Expression.Parameter(typeof(object[])); //  Аргументы метода
            var parameters = method.GetParameters();
            Expression body = Expression.Call(
                method.IsStatic
                    ? null
                    : Expression.Convert(targetArg, method.DeclaringType), //  тип в котором объявлен метод
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
        /// Оборачивает вызов делегата
        /// </summary>
        /// <param name="handler">Оборачиваемый делегат</param>
        /// <returns>Кортеж с идентификатором выражения и делегатом</returns>
        protected static Tuple<string, Invoker> CreateCompiledExpression(Delegate handler)
        {
            return CreateCompiledExpression(handler.GetMethodInfo());
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Идентификатр однозначно определяющий метод на уровне типа (но не на глобальном уровне)
        /// </summary>
        /// <param name="name">Имя метода</param>
        /// <param name="argsTypes">Типы аргументов метода</param>
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

        #endregion

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

        public IEnumerable<string> Configure(Type type)
        {
            var result = type.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> Configure(Type type, string methodName)
        {
            var result = type.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(m => m.Name.Equals(methodName))
                ?.Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        public IEnumerable<string> ConfigureGeneric<T>(Type type, string methodName)
        {
            var result = type.GetMethods(BindingFlags.Static |
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

        public IEnumerable<string> ConfigureGeneric<T>(Type type, Func<MethodInfo, bool> filter)
        {
            var result = type.GetMethods(BindingFlags.Static |
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

        public IEnumerable<string> Configure(Type type, Func<MethodInfo, bool> filter)
        {
            var result = type.GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)?.Where(filter)
                ?.Select(CreateCompiledExpression);
            Configure(result);
            return result.Select(r => r.Item1).ToList();
        }

        #endregion

        #region Configure by MethodInfo

        /// <summary>
        /// Вносит в кэш вызов указанного метода
        /// </summary>
        /// <param name="method">Метод</param>
        /// <returns>Идентификатор для вызова</returns>
        public string Configure(MethodInfo method)
        {
            var invoke = CreateCompiledExpression(method);
            Configure(invoke);
            return invoke.Item1;
        }

        /// <summary>
        /// Вносит в кэш вызов указанного делегата
        /// </summary>
        /// <param name="handler">Делегат</param>
        /// <returns>Идентификатор вызова</returns>
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

        #endregion

        #region Configuration

        /// <summary>
        /// Наполнение кэша из списка методов с идентификаторами
        /// </summary>
        protected void Configure(IEnumerable<Tuple<string, Invoker>> list)
        {
            foreach (var invoke in list)
            {
                Configure(invoke);
            }
        }

        /// <summary>
        /// Добавление вызова в кэш
        /// </summary>
        protected void Configure(Tuple<string, Invoker> invoke)
        {
            _invokeCachee[invoke.Item1] = invoke.Item2;
        }

        #endregion

        #region Invoking

        /// <summary>
        /// Вызов статического метода по идентификатору, в случае отсутствия метода в кеше будет брошено исключение KeyNotFoundException
        /// </summary>
        /// <param name="identity">Идентификатор метода</param>
        /// <param name="args">Аргументы метода</param>
        /// <returns>Результат выполнения</returns>
        public object InvokeStatic(string identity, object[] args)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity](null, args);
            }

            throw new KeyNotFoundException(String.Format("Not found method with identity '{0}'", identity));
        }

        /// <summary>
        /// Вызов метода по идентификатору, в случае отсутствия метода в кеше будет брошено исключение KeyNotFoundException
        /// </summary>
        /// <param name="target">Инстанс на котором вызывается метод</param>
        /// <param name="identity">Идентификатор метода</param>
        /// <param name="args">Аргументы метода</param>
        /// <returns>Результат выполнения</returns>
        public object Invoke(object target, string identity, object[] args)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity](target, args);
            }

            throw new KeyNotFoundException(String.Format("Not found method with identity '{0}'", identity));
        }

        public object Invoke(object target, string identity)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity](target, null);
            }

            throw new KeyNotFoundException(String.Format("Not found method with identity '{0}'", identity));
        }

        /// <summary>
        /// Выполнение статического закэшированного метода
        /// </summary>
        /// <param name="methodName">Имя метода</param>
        /// <param name="args">Аргументы метода</param>
        /// /// <returns>Результат выполнения</returns>
        public object Invoke(string methodName, object[] args)
        {
            return InvokeStatic(CreateMethodIdentity(methodName, args.Select(a => a.GetType()).ToArray()), args);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Запрос идентификатора для метода
        /// </summary>
        /// <param name="methodName">Имя метода</param>
        /// <param name="argsTypes">Список типов аргументов метода</param>
        /// <returns>Идентификатор</returns>
        public string GetInvokerIdentity(string methodName, params Type[] argsTypes)
        {
            return CreateMethodIdentity(methodName, argsTypes);
        }

        /// <summary>
        /// Запрос делегата оборачивающего метод
        /// </summary>
        /// <param name="identity">Идентификатор метода</param>
        /// <returns>Делегат</returns>
        public Invoker GetInvoker(string identity)
        {
            if (_invokeCachee.ContainsKey(identity))
            {
                return _invokeCachee[identity];
            }

            return null;
        }

        /// <summary>
        /// Запрос делегата оборачивающего метод
        /// </summary>
        /// <param name="methodName">Имя метода</param>
        /// <param name="argsTypes">Список типов аргументов метода</param>
        /// <returns>Делегат</returns>
        public Invoker GetInvoker(string methodName, params Type[] argsTypes)
        {
            return GetInvoker(CreateMethodIdentity(methodName, argsTypes));
        }

        #endregion

        #region Factories

        public static IInvokeWrapper Create()
        {
            return new InvokeWrapper();
        }

        #endregion
    }
}
