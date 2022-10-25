using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ZeroLevel.Services
{
    public delegate object DecorateMethodCallHandler(string methodName, params object[] args);

    public class ProxyTypeBuilder
    {
        private enum MethodType
        {
            DirectProxy,
            AbstractOverrideProxy
        }

        #region Fields
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly TypeBuilder _typeBuilder;
        private readonly FieldBuilder _callbackField;
        private readonly IDictionary<string, FieldBuilder> _fields = new Dictionary<string, FieldBuilder>();

        private readonly Type _parentType;
        private readonly Type[] _interfaces;
        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Название создаваемого типа</param>
        /// <param name="parentType">Тип от которого должен наследоваться создаваемый тип, если null, наследование от object</param>
        /// <param name="interfaces">Интерфейсы которые должен реализовать создаваемый тип</param>
        public ProxyTypeBuilder(string typeName, Type parentType, params Type[] interfaces)
        {
            var newAssemblyName = new AssemblyName(Guid.NewGuid().ToString());
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(newAssemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(newAssemblyName.Name);
            _typeBuilder = _moduleBuilder.DefineType(typeName,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.Serializable |
                TypeAttributes.AutoLayout, parentType ?? typeof(object), interfaces);
            // Поле для хранения метода обратного вызова
            _callbackField = _typeBuilder.DefineField("_callbackHandler", CreateDecorateMethodCallHandlerDelegate(_moduleBuilder), FieldAttributes.Private);
            _interfaces = interfaces;
            _parentType = parentType;
        }
        /// <summary>
        /// Собирает конечный тип
        /// </summary>
        /// <returns>Готовый тип</returns>
        public Type Complete()
        {
            // Создание конструктора
            CreateConstructor();
            // Реализация методов интерфейсов
            ProceedInterfaces();
            // Реализация абстрактных методов родительского класса
            ProceedParentAbstractMethods();
            // Сборка типа
            var type = _typeBuilder.CreateType();
            // Результат
            return type;
        }

        #region Private members
        private readonly Dictionary<string, MethodBuilder> _methodsBuilderCachee = new Dictionary<string, MethodBuilder>();
        /// <summary>
        /// Создает конструктор принимающий метод обратного вызова
        /// </summary>
        private void CreateConstructor()
        {
            Type objType = Type.GetType("System.Object");
            ConstructorInfo objCtor = objType.GetConstructor(new Type[0]);
            ConstructorBuilder cBuilder =
                _typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard,
                    new Type[1] { typeof(DecorateMethodCallHandler) });

            ILGenerator cil = cBuilder.GetILGenerator();
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Call, objCtor);
            cil.Emit(OpCodes.Nop);
            cil.Emit(OpCodes.Nop);
            // Присвоение полю класса _callbackHandler аргумента конструктора
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldarg_1);
            cil.Emit(OpCodes.Stfld, _callbackField);
            cil.Emit(OpCodes.Nop);
            cil.Emit(OpCodes.Ret);
        }
        /// <summary>
        /// Создает декортаторы для вызовов интерфейсных методов
        /// </summary>
        private void ProceedInterfaces()
        {
            var list = new List<Type>();
            if (_interfaces != null && _interfaces.Length > 0)
            {
                list.AddRange(_interfaces);
                list.AddRange(GetInterfaces(_interfaces));
            }
            if (_parentType != null)
            {
                list.AddRange(GetInterfaces(new Type[] { _parentType }));
            }
            list = list.Distinct().ToList();
            foreach (var interfaceType in list)
            {
                var properties = interfaceType.GetProperties();
                foreach (var property in properties)
                {
                    _fields.Add(property.Name, _typeBuilder.DefineField("_" + property.Name, property.PropertyType, FieldAttributes.Private));
                }
                foreach (var method in interfaceType.GetMethods())
                {
                    if (properties.Any(p => p.GetGetMethod() == method || p.GetSetMethod() == method))
                    {
                        continue;
                        //CreateProxyMethod(_typeBuilder, method, MethodType.DirectProxy, true);
                    }
                    else
                    {
                        CreateProxyMethod(_typeBuilder, method, MethodType.DirectProxy);
                    }
                }
                foreach (var propertyInfo in properties)
                {
                    AddProperty(_typeBuilder, propertyInfo.Name, propertyInfo.PropertyType);
                }
            }
        }

        private static IEnumerable<Type> GetInterfaces(Type[] sourceTypes)
        {
            var interfaces = new List<Type>();
            IEnumerable<Type> subList = sourceTypes;
            while (subList.Count() != 0)
            {
                subList = subList.SelectMany(i => i.GetInterfaces());
                interfaces.AddRange(subList);
            }
            return interfaces;
        }
        /// <summary>
        /// Создает декораторы для вызовов абстрактных методов родительского класса
        /// </summary>
        private void ProceedParentAbstractMethods()
        {
            if (_parentType != null)
            {
                foreach (var method in _parentType.GetMethods())
                {
                    if (method.IsAbstract)
                    {
                        CreateProxyMethod(_typeBuilder, method, MethodType.AbstractOverrideProxy);
                    }
                }
            }
        }
        /// <summary>
        /// Создание прокси-метода, вызывающего метод обратного вызова, передавая ему аргументы для реального метода
        /// </summary>
        private void CreateProxyMethod(TypeBuilder typeBuilder, MethodInfo method, MethodType methodType, bool isPropertyAccessor = false)
        {
            var methodName = method.Name;
            var returnType = method.ReturnType;
            var parametersTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var hasReturnValue = returnType != typeof(void);
            MethodBuilder dynamicMethod = null;
            switch (methodType)
            {
                case MethodType.DirectProxy:
                    dynamicMethod = typeBuilder.DefineMethod(methodName,
                    method.Attributes ^ MethodAttributes.Abstract,
                    method.CallingConvention,
                    returnType,
                    method.ReturnParameter.GetRequiredCustomModifiers(),
                    method.ReturnParameter.GetOptionalCustomModifiers(),
                    parametersTypes,
                    method.GetParameters().Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                    method.GetParameters().Select(p => p.GetOptionalCustomModifiers()).ToArray());
                    break;
                case MethodType.AbstractOverrideProxy:
                    dynamicMethod = typeBuilder.DefineMethod(methodName,
                    MethodAttributes.Public |
                    MethodAttributes.ReuseSlot |
                    MethodAttributes.Virtual |
                    MethodAttributes.HideBySig |
                    MethodAttributes.Final,
                    method.CallingConvention,
                    returnType,
                    method.ReturnParameter.GetRequiredCustomModifiers(),
                    method.ReturnParameter.GetOptionalCustomModifiers(),
                    parametersTypes,
                    method.GetParameters().Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                    method.GetParameters().Select(p => p.GetOptionalCustomModifiers()).ToArray());
                    break;
            }
            int index = 1;
            foreach (var p in method.GetParameters())
            {
                dynamicMethod.DefineParameter(index, ParameterAttributes.In, p.Name);
                index++;
            }
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _callbackField);

            ParameterInfo[] parameters = method.GetParameters();
            // Массив для параметров оригинального метода
            LocalBuilder argArray = il.DeclareLocal(typeof(object[]));
            il.Emit(OpCodes.Ldc_I4, parameters.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, argArray);
            // Заполнение массива
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo info = parameters[i];
                il.Emit(OpCodes.Ldloc, argArray);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg_S, i + 1);
                if (info.ParameterType.IsPrimitive || info.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, info.ParameterType);
                il.Emit(OpCodes.Stelem_Ref);
            }
            // Аргументы прокси-метода

            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Ldloc, argArray);
            // Вызов прокси-метода
            il.Emit(OpCodes.Callvirt, typeof(DecorateMethodCallHandler).GetMethod("Invoke"));
            // Возврат результата
            if (hasReturnValue)
            {
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, returnType);
                }
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
        }
        /// <summary>
        /// Создает делегат вида public delegate object DecorateMethodCallHandler(string methodName, params object[] args);
        /// </summary>
        private static Type CreateDecorateMethodCallHandlerDelegate(ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DecorateMethodCallHandler",
                TypeAttributes.Class |
                TypeAttributes.Public |
                TypeAttributes.Sealed |
                TypeAttributes.AnsiClass |
                TypeAttributes.AutoClass, typeof(MulticastDelegate));
            var constructorBuilder =
                typeBuilder.DefineConstructor(MethodAttributes.RTSpecialName |
                MethodAttributes.HideBySig |
                MethodAttributes.Public,
                CallingConventions.Standard, new Type[] { typeof(object), typeof(System.IntPtr) });
            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            var methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof(object),
                new[] { typeof(string), typeof(object[]) });
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            return typeBuilder.CreateType();
        }

        private static void AddProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            var getMethod = typeBuilder.DefineMethod("get_" + propertyName,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            var getMethodIL = getMethod.GetILGenerator();
            getMethodIL.Emit(OpCodes.Ldarg_0);
            getMethodIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getMethodIL.Emit(OpCodes.Ret);

            var setMethod = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });
            var setMethodIL = setMethod.GetILGenerator();
            Label modifyProperty = setMethodIL.DefineLabel();
            Label exitSet = setMethodIL.DefineLabel();

            setMethodIL.MarkLabel(modifyProperty);
            setMethodIL.Emit(OpCodes.Ldarg_0);
            setMethodIL.Emit(OpCodes.Ldarg_1);
            setMethodIL.Emit(OpCodes.Stfld, fieldBuilder);

            setMethodIL.Emit(OpCodes.Nop);
            setMethodIL.MarkLabel(exitSet);
            setMethodIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethod);
            propertyBuilder.SetSetMethod(setMethod);
        }
        #endregion
    }

    public delegate object RemoteProcedureCallHandler(string contractName, string methodName, string returnTypeFullName, params object[] args);

    internal static class DynamicProxyGenerator
    {
        #region Private Fields
        private readonly static IDictionary<string, ModuleBuilder> _builders = new Dictionary<string, ModuleBuilder>();
        private readonly static IDictionary<Type, Type> _types = new Dictionary<Type, Type>();
        private readonly static object _lockObject = new object();
        #endregion
        /*
        #region Public Methods
        // Note that calling this method will cause any further
        // attempts to generate an interface to fail
        internal static void Save()
        {
            foreach (var builder in _builders.Select(b => b.Value))
            {
                var ass = (AssemblyBuilder)builder.Assembly;
                try
                {
                    ass.Save(ass.GetName().Name + ".dll");
                }
                catch { }
            }
        }
        #endregion
        */
        #region Private Methods
        /// <summary>
        /// Создание экземпляра прокси класса по интерфейсу <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Интерфейс</typeparam>
        /// <param name="rpcHandler">Обработчик вызова метода</param>
        /// <returns>Экземпляр прокси класса</returns>
        internal static T CreateInterfaceInstance<T>(RemoteProcedureCallHandler rpcHandler)
        {
            var destType = GenerateInterfaceType<T>(rpcHandler);
            return (T)Activator.CreateInstance(destType);
        }
        /// <summary>
        /// Проверка корректности начальных условий
        /// </summary>
        /// <param name="sourceType">Тип интерфейса</param>
        /// <param name="mi">Метод-обработчик удаленного вызова</param>
        private static void Validate(Type sourceType, MethodInfo mi)
        {
            if (!sourceType.IsInterface)
                throw new ArgumentException("Type T is not an interface", "T");
            if ((mi.Attributes & MethodAttributes.Public) != MethodAttributes.Public)
                throw new ArgumentException("Method must be public.", "getter");
        }
        /// <summary>
        /// Получение динамического сбощика модуля
        /// </summary>
        /// <param name="sourceType">Тип интерфейса</param>
        /// <returns>ModuleBuilder</returns>
        private static ModuleBuilder CreateModuleBuilder(Type sourceType)
        {
            var orginalAssemblyName = sourceType.Assembly.GetName().Name;
            ModuleBuilder moduleBuilder;
            if (!_builders.TryGetValue(orginalAssemblyName, out moduleBuilder))
            {
                var newAssemblyName = new AssemblyName(Guid.NewGuid() + "." + orginalAssemblyName);
                var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(newAssemblyName, AssemblyBuilderAccess.RunAndCollect);
                moduleBuilder = dynamicAssembly.DefineDynamicModule(newAssemblyName.Name);
                _builders.Add(orginalAssemblyName, moduleBuilder);
            }
            return moduleBuilder;
        }
        /// <summary>
        /// Создание списка списка интерфейсов, по которому требуется создать прокси класс
        /// В список входит указанный интерфейс <typeparamref name="T"/> и все интерфейсы от которых он унаследован
        /// </summary>
        /// <param name="sourceType">Тип интерфейса</param>
        /// <returns>Список интерфейсов</returns>
        internal static List<Type> GetDistinctInterfaces(Type sourceType)
        {
            var interfaces = new List<Type>();
            IEnumerable<Type> subList = new[] { sourceType };
            while (subList.Count() != 0)
            {
                interfaces.AddRange(subList);
                subList = subList.SelectMany(i => i.GetInterfaces());
            }
            return interfaces.Distinct().ToList();
        }
        /// <summary>
        /// Добавление нового метода в прокси тип
        /// </summary>
        /// <param name="typeBuilder">Сборщик типа</param>
        /// <param name="method">Описание метода</param>
        /// <param name="handler">Прокси метод</param>
        /// <param name="contractName">Контракт к которому относится метод (Тип интерфейса)</param>
        private static void AppendMethodToProxy(TypeBuilder typeBuilder, MethodInfo method, RemoteProcedureCallHandler handler, string contractName)
        {
            string methodName = method.Name;
            Type retType = method.ReturnType;
            bool hasReturnValue = retType != typeof(void);

            var newMethod = typeBuilder.DefineMethod(methodName,
                method.Attributes ^ MethodAttributes.Abstract,
                method.CallingConvention,
                retType,
                method.ReturnParameter.GetRequiredCustomModifiers(),
                method.ReturnParameter.GetOptionalCustomModifiers(),
                method.GetParameters().Select(p => p.ParameterType).ToArray(),
                method.GetParameters().Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                method.GetParameters().Select(p => p.GetOptionalCustomModifiers()).ToArray()
                );

            var il = newMethod.GetILGenerator();

            /* Type exType = typeof(Exception);
             ConstructorInfo exCtorInfo = exType.GetConstructor(new Type[] { typeof(string) });
             MethodInfo exToStrMI = exType.GetMethod("ToString");
             MethodInfo writeLineMI = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string), typeof(object) });
             LocalBuilder tmp2 = il.DeclareLocal(exType);*/

            ParameterInfo[] parameters = method.GetParameters();
            // Массив для параметров оригинального метода
            LocalBuilder argArray = il.DeclareLocal(typeof(object[]));
            il.Emit(OpCodes.Ldc_I4, parameters.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, argArray);
            // Заполнение массива
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo info = parameters[i];
                il.Emit(OpCodes.Ldloc, argArray);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg_S, i + 1);
                if (info.ParameterType.IsPrimitive || info.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, info.ParameterType);
                il.Emit(OpCodes.Stelem_Ref);
            }
            // Аргументы прокси-метода
            il.Emit(OpCodes.Ldstr, contractName);
            il.Emit(OpCodes.Ldstr, methodName);
            if (hasReturnValue)
            {
                il.Emit(OpCodes.Ldstr, retType.FullName);
            }
            else
            {
                il.Emit(OpCodes.Ldstr, typeof(void).FullName);
            }
            il.Emit(OpCodes.Ldloc, argArray);
            // Вызов прокси-метода

            // Label exBlock = il.BeginExceptionBlock();
            il.EmitCall(OpCodes.Call, handler.GetMethodInfo(), null);
            /*
            il.Emit(OpCodes.Stloc_S, tmp2);
            il.Emit(OpCodes.Ldstr, "Caught {0}");
            il.Emit(OpCodes.Ldloc_S, tmp2);
            il.EmitCall(OpCodes.Callvirt, exToStrMI, null);
            il.EmitCall(OpCodes.Call, writeLineMI, null);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.EndExceptionBlock();   
            */
            // Возврат результата
            if (hasReturnValue)
            {
                if (retType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, retType);
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
        }
        /// <summary>
        /// Создание прокси типа для интерфейса <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Интерфейс</typeparam>
        /// <param name="rpcHandler">Прокси метод, для обработки вызова реального метода</param>
        /// <returns>Созданный тип</returns>
        private static Type GenerateInterfaceType<T>(RemoteProcedureCallHandler rpcHandler)
        {
            var sourceType = typeof(T);
            Type newType;
            if (_types.TryGetValue(sourceType, out newType))
                return newType;

            string sourceContractFullName = sourceType.FullName;
            // Make sure the same interface isn't implemented twice
            lock (_lockObject)
            {
                if (_types.TryGetValue(sourceType, out newType))
                    return newType;
                // Validation
                Validate(sourceType, rpcHandler.Method);
                // Module and Assembly Creation
                var moduleBuilder = CreateModuleBuilder(sourceType);
                var assemblyName = moduleBuilder.Assembly.GetName();
                // Create the TypeBuilder
                var typeBuilder = moduleBuilder.DefineType(sourceType.FullName, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { sourceType });
                // Enumerate interface inheritance hierarchy
                var interfaces = GetDistinctInterfaces(sourceType);
                // Create the methods
                foreach (var method in interfaces.SelectMany(i => i.GetMethods()))
                {
                    AppendMethodToProxy(typeBuilder, method, rpcHandler, sourceContractFullName);
                }
                // Create and return the defined type
                newType = typeBuilder.CreateType();
                _types.Add(sourceType, newType);
                return newType;
            }
        }
        #endregion
    }
}
