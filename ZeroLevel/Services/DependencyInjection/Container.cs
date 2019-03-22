using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Patterns.DependencyInjection
{
    internal sealed class Container :
        IContainer
    {
        #region Activator
        private static object Activate(Type type, object[] args)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            CultureInfo culture = null; // use InvariantCulture or other if you prefer
            return Activator.CreateInstance(type, flags, null, args, culture);
        }

        public T CreateInstance<T>(string resolveName = "")
        {
            T instance = default(T);
            try
            {
                instance = Resolve<T>(resolveName);
            }
            catch
            {
                instance = (T)Activate(typeof(T), null);
            }
            Compose(instance);
            return instance;
        }

        public T CreateInstance<T>(object[] args, string resolveName = "")
        {
            T instance = default(T);
            try
            {
                instance = Resolve<T>(resolveName, args);
            }
            catch
            {
                instance = (T)Activate(typeof(T), args);
            }
            Compose(instance);
            return instance;
        }

        public object CreateInstance(Type type, string resolveName = "")
        {
            object instance = null;
            try
            {
                instance = Resolve(type, resolveName);
            }
            catch
            {
                instance = Activate(type, null);
            }
            Compose(instance);
            return instance;
        }

        public object CreateInstance(Type type, object[] args, string resolveName = "")
        {
            object instance = null;
            try
            {
                instance = Resolve(type, resolveName, args);
            }
            catch
            {
                instance = Activate(type, args);
            }
            Compose(instance);
            return instance;
        }
        #endregion

        #region Caching
        private readonly ReaderWriterLockSlim _rwLock =
            new ReaderWriterLockSlim();
        /// <summary>
        /// Маппинг контракт - резрешения
        /// </summary>
        private readonly Dictionary<Type, List<ResolveTypeInfo>> _resolvingMap =
            new Dictionary<Type, List<ResolveTypeInfo>>();

        private readonly object _constructorCacheeLocker = new object();
        /// <summary>
        /// Кэш данных о контрукторах типа
        /// </summary>
        private readonly Dictionary<Type, IEnumerable<ConstructorMetadata>> _constructorCachee =
            new Dictionary<Type, IEnumerable<ConstructorMetadata>>();
        #endregion

        #region Private
        /// <summary>
        /// Создание экземпляра объекта по указанному разрешению зависимости
        /// </summary>
        /// <param name="resolveType">Метаданные разрешения зависимости</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <returns>Экземпляр объекта</returns>
        private object MakeResolving(ResolveTypeInfo resolveType, object[] args, bool compose = true)
        {
            Type instanceType = resolveType.ImplementationType;
            if (resolveType.IsShared)
            {
                if (null == resolveType.SharedInstance)
                {
                    resolveType.SharedInstance = MakeInstance(instanceType, args ?? resolveType.ConstructorParameters);
                    if (compose)
                    {
                        Compose(resolveType.SharedInstance);
                    }
                }
                return resolveType.SharedInstance;
            }
            var sessionInstance = MakeInstance(instanceType, args ?? resolveType.ConstructorParameters);
            if (compose)
            {
                Compose(sessionInstance);
            }
            return sessionInstance;
        }
        /// <summary>
        /// Создание экземпляра объекта по указанному разрешению зависимости, для обобщенного типа контракта
        /// </summary>
        /// <param name="resolveType">Метаданные разрешения зависимости</param>
        /// <param name="genericType">Обобщенный тип контракта</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <returns>Экземпляр объекта</returns>
        private object MakeGenericResolving(ResolveTypeInfo resolveType, Type genericType, object[] args, bool compose = true)
        {
            if (null == resolveType.GenericCachee)
            {
                resolveType.GenericCachee = new Dictionary<Type, Type>();
            }
            if (false == resolveType.GenericCachee.ContainsKey(genericType))
            {
                var genericArgumentTypes = genericType.GetGenericArguments();
                var realType = resolveType.ImplementationType.MakeGenericType(genericArgumentTypes);
                resolveType.GenericCachee.Add(genericType, realType);
            }
            Type instanceType = resolveType.GenericCachee[genericType];
            if (resolveType.IsShared)
            {
                if (resolveType.GenericInstanceCachee == null)
                {
                    resolveType.GenericInstanceCachee = new Dictionary<Type, object>();
                }
                if (false == resolveType.GenericInstanceCachee.ContainsKey(instanceType))
                {
                    var sharedInstance = MakeInstance(instanceType, args ?? resolveType.ConstructorParameters);
                    if (compose)
                    {
                        Compose(sharedInstance);
                    }
                    resolveType.GenericInstanceCachee.Add(instanceType, sharedInstance);
                }
                return resolveType.GenericInstanceCachee[instanceType];
            }
            var sessionInstance = MakeInstance(instanceType, args ?? resolveType.ConstructorParameters);
            if (compose)
            {
                Compose(sessionInstance);
            }
            return sessionInstance;
        }
        /// <summary>
        /// Сбор свойств типа помеченных аттрибутом разрешения зависимости
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Список свойств отмеченных аттрибутом Resolve</returns>
        private static IEnumerable<PropertyInfo> CollectResolvingProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy).
                Where(p => p.GetCustomAttribute<ResolveAttribute>() != null);
        }
        /// <summary>
        /// Сбор полей типа помеченных аттрибутом разрешения зависимости
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Список полей отмеченных аттрибутом Resolve</returns>
        private static IEnumerable<FieldInfo> CollectResolvingFields(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy).
                Where(p => p.GetCustomAttribute<ResolveAttribute>() != null);
        }
        /// <summary>
        /// Поиск разрешения зависимости
        /// </summary>
        /// <param name="type">Тип контракта</param>
        /// <param name="resolveName">Имя разрешения зависимости</param>
        /// <param name="contractType">Переопределенный тип контракта</param>
        /// <returns></returns>
        private ResolveTypeInfo FindResolving(Type type, string resolveName, Type contractType)
        {
            HashSet<Type> contract_candidates = new HashSet<Type>();
            if (contractType != null)
            {
                if (contractType.IsInterface)
                    contract_candidates.Add(contractType);
                foreach (var c in GetInterfacesAndAbstracts(contractType))
                    contract_candidates.Add(c);
            }
            if (contract_candidates.Count == 0)
            {
                if (type.IsInterface)
                    contract_candidates.Add(type);
                foreach (var c in GetInterfacesAndAbstracts(type))
                    contract_candidates.Add(c);
            }
            if (contract_candidates.Count > 0)
            {
                try
                {
                    _rwLock.EnterReadLock();
                    var resolveInfo = _resolvingMap.Single(r => contract_candidates.Any(ct => ct == r.Key));
                    return resolveInfo.Value.First(ri => ri.ResolveKey.Equals(resolveName, StringComparison.OrdinalIgnoreCase));
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            throw new KeyNotFoundException(string.Format("Не удалось разрешить зависимость для типа {0} по ключу '{1}'",
                type.FullName, resolveName));
        }
        /// <summary>
        /// Разрешение зависимости по аттрибуту Resolve
        /// </summary>
        /// <param name="type">Тип контракта</param>
        /// <param name="resolveAttribute">Аттрибут</param>
        /// <returns>Экземпляр объекта</returns>
        private object MakeInstanceBy(Type type, ResolveAttribute resolveAttribute)
        {
            var is_generic = false;
            var maybyType = type;
            if (maybyType.IsGenericTypeDefinition)
            {
                maybyType = maybyType.GetGenericTypeDefinition();
                is_generic = true;
            }
            var resolveType = FindResolving(maybyType,
                resolveAttribute?.ResolveName ?? string.Empty,
                resolveAttribute?.ContractType);
            try
            {
                if (is_generic)
                    return MakeGenericResolving(resolveType, type, null);
                return MakeResolving(resolveType, null);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Не удалось создать экземпляр типа {0} для разрешения зависимости типа {1} по ключу {2}",
                    type.FullName, type.FullName, resolveAttribute?.ResolveName), ex);
            }
        }
        /// <summary>
        /// Сбор интерфейсов и абстрактных классов от которых унаследован тип
        /// </summary>
        /// <param name="sourceType">Тип</param>
        /// <returns>Список интерфейсов и абстрактных классов</returns>
        private static IEnumerable<Type> GetInterfacesAndAbstracts(Type sourceType)
        {
            var interfaces = sourceType.GetInterfaces().ToList();
            var parent = sourceType.BaseType;
            while (parent != null && parent != typeof(object))
            {
                if (parent.IsAbstract && parent.IsClass)
                    interfaces.Add(parent);
                parent = parent.BaseType;
            }
            return interfaces;
        }
        /// <summary>
        /// Получение списка метаданных по конструкторам типа
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Метаданные о конструкторах типа</returns>
        private IEnumerable<ConstructorMetadata> GetConstructors(Type type)
        {
            lock (_constructorCacheeLocker)
            {
                if (false == _constructorCachee.ContainsKey(type))
                {
                    var list = new List<ConstructorMetadata>();
                    foreach (var c in type.
                        GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                    {
                        list.Add(new ConstructorMetadata(this, c));
                    }
                    _constructorCachee.Add(type, list);
                }
            }
            return _constructorCachee[type];
        }
        /// <summary>
        /// Создание экземпляра объекта, в том числе с непубличными конструкторами
        /// </summary>
        /// <param name="type">Тип</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <returns>Экземпляр типа</returns>
        private object MakeInstance(Type type, object[] args)
        {
            ConstructorInfo constructor = null;
            object[] parameters = null;
            foreach (var ctor in GetConstructors(type))
            {
                if (ctor.IsMatch(args, out parameters))
                {
                    constructor = ctor.Constructor;
                    break;
                }
            }
            if (null == constructor)
            {
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            }
            else
            {
                return constructor.Invoke(parameters);
                /*var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                CultureInfo culture = null; // use InvariantCulture or other if you prefer
                return Activator.CreateInstance(type, flags, null, args, culture);*/
            }
        }
        /// <summary>
        /// Регистрация разрешения зависимости
        /// </summary>
        /// <param name="contractType">Тип контракта</param>
        /// <param name="resolveType">Метаданные разрешения</param>
        private void Register(Type contractType, ResolveTypeInfo resolveType)
        {
            try
            {
                _rwLock.EnterUpgradeableReadLock();
                if (false == _resolvingMap.ContainsKey(contractType))
                {
                    try
                    {
                        _rwLock.EnterWriteLock();
                        _resolvingMap.Add(contractType, new List<ResolveTypeInfo>());
                    }
                    finally
                    {
                        _rwLock.ExitWriteLock();
                    }
                }
                else
                {
                    if (resolveType.IsDefault &&
                        _resolvingMap[contractType].Any(it => it.IsDefault))
                    {
                        throw new Exception(
                            string.Format("Default resolve type already has been defined. Contract: {0}", contractType.FullName));
                    }
                    if (_resolvingMap[contractType].
                        Any(it => it.ResolveKey.Equals(resolveType.ResolveKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new Exception(
                            string.Format("Resolve type with the same name '{0}' already has been defined. Contract: {1}", resolveType.ResolveKey, contractType.FullName));
                    }
                }
                try
                {
                    _rwLock.EnterWriteLock();
                    _resolvingMap[contractType].Add(resolveType);
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
            finally
            {
                _rwLock.ExitUpgradeableReadLock();
            }
        }
        #endregion

        #region Register
        public void Register<TContract, TImplementation>()
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = true,
                IsShared = false,
                ResolveKey = string.Empty
            };
            Register(typeof(TContract), resolveType);
        }

        public void Register<TContract, TImplementation>(bool shared)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = true,
                IsShared = shared,
                ResolveKey = string.Empty
            };
            Register(typeof(TContract), resolveType);
        }

        public void Register<TContract, TImplementation>(string resolveName)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = false,
                ResolveKey = resolveName?.Trim()
            };
            Register(typeof(TContract), resolveType);
        }

        public void Register<TContract, TImplementation>(string resolveName, bool shared)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = shared,
                ResolveKey = resolveName?.Trim()
            };
            Register(typeof(TContract), resolveType);
        }

        public void Register(Type contractType, Type implementationType)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = true,
                IsShared = false,
                ResolveKey = string.Empty
            };
            Register(contractType, resolveType);
        }

        public void Register(Type contractType, Type implementationType, string resolveName)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = false,
                ResolveKey = resolveName?.Trim()
            };
            Register(contractType, resolveType);
        }

        public void Register(Type contractType, Type implementationType, bool shared)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = true,
                IsShared = shared,
                ResolveKey = string.Empty
            };
            Register(contractType, resolveType);
        }

        public void Register(Type contractType, Type implementationType, string resolveName, bool shared)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = shared,
                ResolveKey = resolveName?.Trim()
            };
            Register(contractType, resolveType);
        }
        #endregion 

        #region Register with parameters
        public void ParameterizedRegister<TContract, TImplementation>(object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = true,
                IsShared = false,
                ResolveKey = string.Empty,
                ConstructorParameters = constructorParameters
            };
            Register(typeof(TContract), resolveType);
        }

        public void ParameterizedRegister<TContract, TImplementation>(string resolveName, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = false,
                ResolveKey = resolveName?.Trim(),
                ConstructorParameters = constructorParameters
            };
            Register(typeof(TContract), resolveType);
        }

        public void ParameterizedRegister<TContract, TImplementation>(bool shared, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = true,
                IsShared = shared,
                ResolveKey = string.Empty,
                ConstructorParameters = constructorParameters
            };
            Register(typeof(TContract), resolveType);
        }

        public void ParameterizedRegister<TContract, TImplementation>(string resolveName, bool shared, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = typeof(TImplementation),
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = shared,
                ResolveKey = resolveName?.Trim(),
                ConstructorParameters = constructorParameters
            };
            Register(typeof(TContract), resolveType);
        }

        public void ParameterizedRegister(Type contractType, Type implementationType, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = true,
                IsShared = false,
                ResolveKey = string.Empty,
                ConstructorParameters = constructorParameters
            };
            Register(contractType, resolveType);
        }

        public void ParameterizedRegister(Type contractType, Type implementationType, string resolveName, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = false,
                ResolveKey = resolveName?.Trim(),
                ConstructorParameters = constructorParameters
            };
            Register(contractType, resolveType);
        }

        public void ParameterizedRegister(Type contractType, Type implementationType, bool shared, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = true,
                IsShared = shared,
                ResolveKey = string.Empty,
                ConstructorParameters = constructorParameters
            };
            Register(contractType, resolveType);
        }

        public void ParameterizedRegister(Type contractType, Type implementationType, string resolveName, bool shared, object[] constructorParameters)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementationType,
                IsDefault = string.IsNullOrWhiteSpace(resolveName),
                IsShared = shared,
                ResolveKey = resolveName?.Trim(),
                ConstructorParameters = constructorParameters
            };
            Register(contractType, resolveType);
        }
        #endregion

        #region Register instance
        /// <summary>
        /// Регистрация готового экземпляра (синглтон)
        /// </summary>
        /// <typeparam name="TContract">Тип контракта</typeparam>
        /// <param name="implementation">Экземпляр</param>
        public void Register<TContract>(TContract implementation)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementation.GetType(),
                IsDefault = true,
                IsShared = true,
                ResolveKey = string.Empty,
                SharedInstance = implementation
            };
            Register(typeof(TContract), resolveType);
        }
        public void Register(Type contractType, object implementation)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementation.GetType(),
                IsDefault = true,
                IsShared = true,
                ResolveKey = string.Empty,
                SharedInstance = implementation
            };
            Register(contractType, resolveType);
        }
        public void Register<TContract>(TContract implementation, string resolveName)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementation.GetType(),
                IsDefault = string.IsNullOrEmpty(resolveName),
                IsShared = true,
                ResolveKey = resolveName,
                SharedInstance = implementation
            };
            Register(typeof(TContract), resolveType);
        }
        public void Register(Type contractType, string resolveName, object implementation)
        {
            var resolveType = new ResolveTypeInfo
            {
                ImplementationType = implementation.GetType(),
                IsDefault = true,
                IsShared = true,
                ResolveKey = resolveName,
                SharedInstance = implementation
            };
            Register(contractType, resolveType);
        }
        #endregion

        #region Safe register
        public bool TryRegister<TContract, TImplementation>(Action<Exception> fallback = null)
        {
            try
            {
                Register<TContract, TImplementation>();
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister<TContract, TImplementation>(bool shared, Action<Exception> fallback = null)
        {
            try
            {
                Register<TContract, TImplementation>(shared);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister<TContract, TImplementation>(string resolveName, Action<Exception> fallback = null)
        {
            try
            {
                Register<TContract, TImplementation>(resolveName);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister<TContract, TImplementation>(string resolveName, bool shared, Action<Exception> fallback = null)
        {
            try
            {
                Register<TContract, TImplementation>(resolveName, shared);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister(Type contractType, Type implementationType, Action<Exception> fallback = null)
        {
            try
            {
                Register(contractType, implementationType);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister(Type contractType, Type implementationType, string resolveName, Action<Exception> fallback = null)
        {
            try
            {
                Register(contractType, implementationType, resolveName);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister(Type contractType, Type implementationType, bool shared, Action<Exception> fallback = null)
        {
            try
            {
                Register(contractType, implementationType, shared);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryRegister(Type contractType, Type implementationType, string resolveName, bool shared, Action<Exception> fallback = null)
        {
            try
            {
                Register(contractType, implementationType, resolveName, shared);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }
        #endregion

        #region Safe register with parameters
        public bool TryParameterizedRegister<TContract, TImplementation>(object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister<TContract, TImplementation>(constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister<TContract, TImplementation>(string resolveName, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister<TContract, TImplementation>(resolveName, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister<TContract, TImplementation>(bool shared, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister<TContract, TImplementation>(shared, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister<TContract, TImplementation>(string resolveName, bool shared, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister<TContract, TImplementation>(resolveName, shared, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister(Type contractType, Type implementationType, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister(contractType, implementationType, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister(Type contractType, Type implementationType, string resolveName, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister(contractType, implementationType, resolveName, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister(Type contractType, Type implementationType, bool shared, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister(contractType, implementationType, shared, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }

        public bool TryParameterizedRegister(Type contractType, Type implementationType, string resolveName, bool shared, object[] constructorParameters, Action<Exception> fallback = null)
        {
            try
            {
                ParameterizedRegister(contractType, implementationType, resolveName, shared, constructorParameters);
                return true;
            }
            catch (Exception ex)
            {
                fallback?.Invoke(ex);
                return false;
            }
        }
        #endregion

        #region Resolving
        public object Resolve(Type type, bool compose = true)
        {
            return Resolve(type, string.Empty, null, compose);
        }

        public object Resolve(Type type, object[] args, bool compose = true)
        {
            return Resolve(type, string.Empty, args, compose);
        }

        public object Resolve(Type type, string resolveName, bool compose = true)
        {
            return Resolve(type, resolveName, null, compose);
        }

        public T Resolve<T>(bool compose = true)
        {
            return (T)Resolve(typeof(T), string.Empty, null, compose);
        }

        public T Resolve<T>(object[] args, bool compose = true)
        {
            return (T)Resolve(typeof(T), string.Empty, args, compose);
        }

        public T Resolve<T>(string resolveName, bool compose = true)
        {
            return (T)Resolve(typeof(T), resolveName, null, compose);
        }

        public T Resolve<T>(string resolveName, object[] args, bool compose = true)
        {
            return (T)Resolve(typeof(T), resolveName, args, compose);
        }

        public bool IsResolvingExists<T>()
        {
            return IsResolvingExists(typeof(T), string.Empty);
        }

        public bool IsResolvingExists<T>(string resolveName)
        {
            return IsResolvingExists(typeof(T), resolveName);
        }

        public bool IsResolvingExists(Type type)
        {
            return IsResolvingExists(type, string.Empty);
        }

        public bool IsResolvingExists(Type type, string resolveName)
        {
            return GetResolvedType(type, resolveName)?.Item1 != null;
        }

        private Tuple<ResolveTypeInfo, bool> GetResolvedType(Type type, string resolveName)
        {
            try
            {
                _rwLock.EnterReadLock();
                if (_resolvingMap.ContainsKey(type))
                {
                    return Tuple.Create(_resolvingMap[type].
                        FirstOrDefault(i => i.ResolveKey.Equals(resolveName, StringComparison.Ordinal)),
                            false);
                }
                else if (type.IsGenericType)
                {
                    var generic_type = type.GetGenericTypeDefinition();
                    if (_resolvingMap.ContainsKey(generic_type))
                    {
                        return Tuple.Create(_resolvingMap[generic_type].
                            FirstOrDefault(i => i.ResolveKey.Equals(resolveName, StringComparison.Ordinal)),
                                true);
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return new Tuple<ResolveTypeInfo, bool>(null, false);
        }

        public object Resolve(Type type, string resolveName, object[] args, bool compose = true)
        {
            var resolve = GetResolvedType(type, resolveName);
            if (null == resolve.Item1)
                throw new KeyNotFoundException(string.Format("Can'r resolve type {0} on key '{1}'",
                    type.FullName, resolveName));
            // Detect instance type
            try
            {
                if (resolve.Item2)
                {
                    return MakeGenericResolving(resolve.Item1, type, args, compose);
                }
                return MakeResolving(resolve.Item1, args, compose);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Can't create instance for type {0} for resolved dependency {1} by key {2}",
                        type.FullName, type.FullName, resolveName), ex);
            }
        }
        #endregion

        #region Safe resolving
        public object TryResolve(Type type, out object result, bool compose = true)
        {
            return TryResolve(type, string.Empty, null, out result, compose);
        }

        public object TryResolve(Type type, object[] args, out object result, bool compose = true)
        {
            return TryResolve(type, string.Empty, args, out result, compose);
        }

        public object TryResolve(Type type, string resolveName, out object result, bool compose = true)
        {
            return TryResolve(type, resolveName, null, out result, compose);
        }

        public bool TryResolve<T>(out T result, bool compose = true)
        {
            object instance;
            if (TryResolve(typeof(T), string.Empty, null, out instance, compose))
            {
                result = (T)instance;
                return true;
            }
            result = default(T);
            return false;
        }

        public bool TryResolve<T>(object[] args, out T result, bool compose = true)
        {
            object instance;
            if (TryResolve(typeof(T), string.Empty, args, out instance, compose))
            {
                result = (T)instance;
                return true;
            }
            result = default(T);
            return false;
        }

        public bool TryResolve<T>(string resolveName, out T result, bool compose = true)
        {
            object instance;
            if (TryResolve(typeof(T), resolveName, null, out instance, compose))
            {
                result = (T)instance;
                return true;
            }
            result = default(T);
            return false;
        }

        public bool TryResolve<T>(string resolveName, object[] args, out T result, bool compose = true)
        {
            object instance;
            if (TryResolve(typeof(T), resolveName, args, out instance, compose))
            {
                result = (T)instance;
                return true;
            }
            result = default(T);
            return false;
        }

        public bool TryResolve(Type type, string resolveName, object[] args, out object result, bool compose = true)
        {
            // Detect instance type
            var resolve = GetResolvedType(type, resolveName);
            if (null == resolve.Item1)
            {
                result = null;
                return false;
            }
            try
            {
                if (resolve.Item2)
                {
                    result = MakeGenericResolving(resolve.Item1, type, args, compose);
                }
                else
                {
                    result = MakeResolving(resolve.Item1, args, compose);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemWarning(
                    string.Format("Не удалось создать экземпляр типа {0} для разрешения зависимости типа {1} по ключу {2}",
                    type.FullName, type.FullName, resolveName), ex);
            }
            result = null;
            return false;
        }
        #endregion

        #region Composition
        /// <summary>
        /// Заполнение полей и свойств объекта отмеченных флагом автоподставновки значений из параметров контейнера
        /// </summary>
        private void FillParametrizedFieldsAndProperties(object instance)
        {
            foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                var attr = property.GetCustomAttribute<ParameterAttribute>();
                if (attr != null)
                {
                    var parameterType = attr.Type ?? property.PropertyType;
                    var parameterName = attr.Name ?? property.Name;
                    property.SetValue(instance, this.Get(parameterType, parameterName));
                }
            }
            foreach (var field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                var attr = field.GetCustomAttribute<ParameterAttribute>();
                if (attr != null)
                {
                    var parameterType = attr.Type ?? field.FieldType;
                    var parameterName = string.IsNullOrWhiteSpace(attr.Name) ? field.Name : attr.Name;
                    field.SetValue(instance, this.Get(parameterType, parameterName));
                }
            }
        }

        private void ComposeParts(object instance)
        {
            var resolve_properties = CollectResolvingProperties(instance.GetType());
            var resolve_fields = CollectResolvingFields(instance.GetType());
            foreach (var p in resolve_properties)
            {
                var resolve_instance = MakeInstanceBy(p.PropertyType,
                    p.GetCustomAttribute<ResolveAttribute>());
                p.SetValue(instance, resolve_instance);
            }
            foreach (var f in resolve_fields)
            {
                var resolve_instance = MakeInstanceBy(f.FieldType,
                    f.GetCustomAttribute<ResolveAttribute>());
                f.SetValue(instance, resolve_instance);
            }
            FillParametrizedFieldsAndProperties(instance);
        }

        private void RecursiveCompose(object instance, HashSet<object> set)
        {
            foreach (var f in
                instance.GetType().GetFields(BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance))
            {
                if (f.FieldType.IsClass || f.FieldType.IsInterface)
                {
                    var next = f.GetValue(instance);
                    if (null != next)
                    {
                        if (set.Add(next))
                        {
                            RecursiveCompose(next, set);
                        }
                    }
                }
            }
            ComposeParts(instance);
        }

        public void Compose(object instance, bool recursive = true)
        {
            if (recursive)
            {
                RecursiveCompose(instance, new HashSet<object>());
            }
            else
            {
                ComposeParts(instance);
            }
        }

        public bool TryCompose(object instanse, bool recursive = true)
        {
            try
            {
                Compose(instanse, recursive);
                return true;
            }
            catch { }
            return false;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            try
            {
                _rwLock.EnterWriteLock();
                foreach (var list in _resolvingMap.Values)
                {
                    foreach (var item in list)
                    {
                        try
                        {
                            (item.SharedInstance as IDisposable)?.Dispose();
                        }
                        catch { }
                        if (item.GenericInstanceCachee != null)
                        {
                            foreach (var gitem in item.GenericInstanceCachee.Values)
                            {
                                try
                                {
                                    (gitem as IDisposable)?.Dispose();
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
        #endregion

        #region IEverythingStorage
        private readonly Lazy<IEverythingStorage> _everything =
            new Lazy<IEverythingStorage>(EverythingStorage.Create);

        public void Save<T>(string key, T value)
        {
            _everything.Value.Add<T>(key, value);
        }
        public void Remove<T>(string key)
        {
            _everything.Value.Remove<T>(key);
        }
        public bool Contains<T>(string key)
        {
            return _everything.Value.ContainsKey<T>(key);
        }
        public bool TrySave<T>(string key, T value)
        {
            return _everything.Value.TryAdd<T>(key, value);
        }
        public bool TryRemove<T>(string key)
        {
            return _everything.Value.TryRemove<T>(key);
        }

        public T Get<T>(string key)
        {
            return _everything.Value.Get<T>(key);
        }

        public object Get(Type type, string key)
        {
            MethodInfo method = typeof(IEverythingStorage).GetMethod("Get");
            MethodInfo genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(_everything.Value, new object[] { key });
        }

        public void SaveOrUpdate<T>(string key, T value)
        {
            _everything.Value.AddOrUpdate<T>(key, value);
        }

        public T GetOrDefault<T>(string key)
        {
            if (_everything.Value.ContainsKey<T>(key))
                return _everything.Value.Get<T>(key);
            return default(T);
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            if (_everything.Value.ContainsKey<T>(key))
                return _everything.Value.Get<T>(key);
            return defaultValue;
        }
        #endregion
    }
}
