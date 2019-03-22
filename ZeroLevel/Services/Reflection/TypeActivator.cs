using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Services.Reflection
{
    public static class TypeActivator
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, Delegate>> _withArgumentsConstructorFactories =
            new ConcurrentDictionary<Type, Dictionary<string, Delegate>>();

        private static readonly IEverythingStorage _withoutArgumentsConstructorFactories = EverythingStorage.Create();

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

        public static T Create<T>(params object[] args)
        {
            var type = typeof(T);
            if (false == _withArgumentsConstructorFactories.ContainsKey(type))
            {
                var d = new Dictionary<string, Delegate>();
                foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public))
                {
                    var invoker = MakeFactory(ctor);
                    if (invoker != null)
                        d.Add(invoker.Item1, invoker.Item2);
                }
                _withArgumentsConstructorFactories.AddOrUpdate(type, d, (k, v) => v);
            }
            var id = CreateMethodIdentity(".ctor",
                args == null ? null : args.Select(a => a.GetType()).ToArray());
            if (_withArgumentsConstructorFactories[type].ContainsKey(id))
                return (T)_withArgumentsConstructorFactories[type][id].DynamicInvoke(args);
            return default(T);
        }

        public static T Create<T>()
            where T : new()
        {
            var type = typeof(T);
            if (false == _withoutArgumentsConstructorFactories.ContainsKey<Func<T>>(type.FullName))
            {
                _withoutArgumentsConstructorFactories.Add<Func<T>>(type.FullName, MakeFactory<T>());
            }
            return _withoutArgumentsConstructorFactories.
                Get<Func<T>>(type.FullName).
                Invoke();
        }

        private static Func<T> MakeFactory<T>()
            where T : new()
        {
            Expression<Func<T>> expr = () => new T();
            NewExpression newExpr = (NewExpression)expr.Body;
            var method = new DynamicMethod(
                name: "lambda",
                returnType: newExpr.Type,
                parameterTypes: new Type[0],
                m: typeof(T).Module,
                skipVisibility: true);
            ILGenerator ilGen = method.GetILGenerator();
            // Constructor for value types could be null
            if (newExpr.Constructor != null)
            {
                ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
            }
            else
            {
                LocalBuilder temp = ilGen.DeclareLocal(newExpr.Type);
                ilGen.Emit(OpCodes.Ldloca, temp);
                ilGen.Emit(OpCodes.Initobj, newExpr.Type);
                ilGen.Emit(OpCodes.Ldloc, temp);
            }
            ilGen.Emit(OpCodes.Ret);
            return (Func<T>)method.CreateDelegate(typeof(Func<T>));
        }

        private static Tuple<string, Delegate> MakeFactory(ConstructorInfo ctor)
        {
            var arguments = ctor.GetParameters();
            var constructorArgumentTypes = arguments.
                Select(a => a.ParameterType).
                ToArray();
            if (constructorArgumentTypes.Any(t => t.IsPointer)) return null;
            var lamdaParameterExpressions = constructorArgumentTypes.
                Select(Expression.Parameter).
                ToArray();
            var constructorParameterExpressions = lamdaParameterExpressions
                .Take(constructorArgumentTypes.Length)
                .ToArray();

            var constructorCallExpression = Expression.New(ctor,
                constructorParameterExpressions);

            var lambda = Expression.Lambda(constructorCallExpression,
                                   lamdaParameterExpressions);

            var identity = CreateMethodIdentity(ctor.Name,
                arguments.Select(p => p.ParameterType).ToArray());
            return new Tuple<string, Delegate>(identity.ToString(), lambda.Compile());
        }
    }
}
