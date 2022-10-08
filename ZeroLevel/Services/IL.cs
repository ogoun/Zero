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
}
