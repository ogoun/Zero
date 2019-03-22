﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZeroLevel.Patterns.DependencyInjection
{
    /// <summary>
    /// Метаданные конструктора
    /// </summary>
    internal class ConstructorMetadata
    {
        public ConstructorInfo Constructor { get; }
        private IReadOnlyList<ConstructorParameter> Parameters { get; }
        private readonly IContainer _parent;

        public ConstructorMetadata(Container parent, ConstructorInfo info)
        {
            _parent = parent;
            Constructor = info;
            Parameters = info.
                GetParameters().
                Select(p =>
                {
                    var parameterAttribute = p.GetCustomAttribute<ParameterAttribute>();
                    var resolveAttribute = p.GetCustomAttribute<ResolveAttribute>();

                    var kind = (parameterAttribute != null) ? ConstructorParameterKind.Parameter :
                    (resolveAttribute != null) ? ConstructorParameterKind.Resolve : ConstructorParameterKind.None;

                    return new ConstructorParameter
                    {
                        Type = p.ParameterType,
                        ParameterKind = kind,
                        ParameterResolveName = (kind == ConstructorParameterKind.Parameter) ? parameterAttribute?.Name ?? p.Name :
                            (kind == ConstructorParameterKind.Resolve) ? resolveAttribute?.ResolveName : null,
                        ParameterResolveType = (kind == ConstructorParameterKind.Parameter) ? parameterAttribute?.Type ?? p.ParameterType :
                            (kind == ConstructorParameterKind.Resolve) ? resolveAttribute?.ContractType ?? p.ParameterType : null,
                        IsNullable = IsNullable(p.ParameterType)
                    };
                }).ToList();
        }

        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }
        /// <summary>
        /// Определение, подходит ли конструктор под указанные аргументы
        /// </summary>
        /// <param name="args">Аргументы</param>
        /// <param name="parameters">Подготовленные массив аргументов для вызова конструктора</param>
        /// <returns>true - если конструктор можно вызвать с переданными аргументами</returns>
        public bool IsMatch(object[] args, out object[] parameters)
        {
            parameters = null;
            int arg_index = 0;
            if (Parameters.Count > 0)
            {
                parameters = new object[Parameters.Count];
                for (int i = 0; i < parameters.Length; i++)
                {
                    switch (Parameters[i].ParameterKind)
                    {
                        case ConstructorParameterKind.Parameter:
                            parameters[i] = _parent.Get(Parameters[i].ParameterResolveType, Parameters[i].ParameterResolveName);
                            break;
                        case ConstructorParameterKind.Resolve:
                            parameters[i] = _parent.Resolve(Parameters[i].ParameterResolveType, Parameters[i].ParameterResolveName);
                            break;
                        default:
                            if (args == null || arg_index >= args.Length) return false;
                            if (null == args[arg_index])
                            {
                                if (Parameters[i].IsNullable)
                                {
                                    parameters[i] = args[arg_index];
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else if (Parameters[i].Type.IsAssignableFrom(args[arg_index].GetType()))
                            {
                                parameters[i] = args[arg_index];
                            }
                            else
                            {
                                try
                                {
                                    parameters[i] = Convert.ChangeType(args[i], Parameters[i].Type);
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                            arg_index++;
                            break;
                    }
                }
                return true;
            }
            if (args != null && args.Length > 0)
                return false;
            return true;
        }
    }
}
