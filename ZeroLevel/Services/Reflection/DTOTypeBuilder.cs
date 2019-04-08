using System;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;

namespace ZeroLevel.Services.Reflection
{
    /// <summary>
    /// Конструктор простейших типов, без методов
    /// </summary>
    public sealed class DTOTypeBuilder
    {
        #region Fields
        private readonly TypeBuilder _typeBuilder;
        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="typeName">Название создаваемого типа</param>
        public DTOTypeBuilder(string typeName)
        {
            var newAssemblyName = new AssemblyName(Guid.NewGuid().ToString());
            var assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(newAssemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(newAssemblyName.Name);
            _typeBuilder = moduleBuilder.DefineType(typeName,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.Serializable |
                TypeAttributes.AutoLayout, typeof(object));

            var a_ctor = typeof(DataContractAttribute).GetConstructor(new Type[] { });
            var a_builder = new CustomAttributeBuilder(a_ctor, new object[] { });
            _typeBuilder.SetCustomAttribute(a_builder);
        }

        public void AppendField<T>(string name)
        {
            _typeBuilder.DefineField(name, typeof(T), FieldAttributes.Public);
        }

        public void AppendField<T>(string name, T defaultValue)
        {
            var builder = _typeBuilder.DefineField(name, typeof(T), FieldAttributes.Public | FieldAttributes.HasDefault);
            builder.SetConstant(defaultValue);
        }

        public void AppendProperty<T>(string name)
        {
            CreateProperty<T>(name, _typeBuilder, null);
        }

        public void AppendProperty<T>(string name, T defaultValue)
        {
            CreateProperty<T>(name, _typeBuilder, null).SetConstant(defaultValue);
        }

        public void AppendProperty<T>(string name, string description)
        {
            CreateProperty<T>(name, _typeBuilder, description);
        }

        public void AppendProperty<T>(string name, string description, T defaultValue)
        {
            CreateProperty<T>(name, _typeBuilder, description).SetConstant(defaultValue);
        }

        public void AppendProperty(Type propertyType, string name)
        {
            CreateProperty(propertyType, name, _typeBuilder, null);
        }

        public void AppendProperty(Type propertyType, string name, object defaultValue)
        {
            CreateProperty(propertyType, name, _typeBuilder, null).SetConstant(defaultValue);
        }

        public void AppendProperty(Type propertyType, string name, string description)
        {
            CreateProperty(propertyType, name, _typeBuilder, description);
        }

        public void AppendProperty(Type propertyType, string name, string description, object defaultValue)
        {
            CreateProperty(propertyType, name, _typeBuilder, description).SetConstant(defaultValue);
        }

        private static FieldBuilder CreateProperty<T>(string name,
            TypeBuilder typeBuilder,
            string description)
        {
            return CreateProperty(typeof(T), name, typeBuilder, description);
        }

        private static FieldBuilder CreateProperty(Type propertyType, string name,
            TypeBuilder typeBuilder,
            string description)
        {
            var backingFieldBuilder = typeBuilder.DefineField("f__" + name.ToLowerInvariant(), propertyType, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault,
                propertyType, new Type[] { propertyType });
            // Build setter
            var getterMethodBuilder = typeBuilder.DefineMethod("get_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType, Type.EmptyTypes);
            var getterIl = getterMethodBuilder.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, backingFieldBuilder);
            getterIl.Emit(OpCodes.Ret);
            // Build setter
            var setterMethodBuilder = typeBuilder.DefineMethod("set_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, new[] { propertyType });
            var setterIl = setterMethodBuilder.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, backingFieldBuilder);
            setterIl.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getterMethodBuilder);
            propertyBuilder.SetSetMethod(setterMethodBuilder);
            // Set description attribute
            if (false == string.IsNullOrWhiteSpace(description))
            {
                var ctorParams = new[] { typeof(string) };
                var classCtorInfo = typeof(DescriptionAttribute).GetConstructor(ctorParams);
                var myCABuilder = new CustomAttributeBuilder(classCtorInfo, new object[] { description });
                propertyBuilder.SetCustomAttribute(myCABuilder);
            }
            var a_ctor = typeof(DataMemberAttribute).GetConstructor(new Type[] { });
            var a_builder = new CustomAttributeBuilder(a_ctor, new object[] { });
            propertyBuilder.SetCustomAttribute(a_builder);
            return backingFieldBuilder;
        }

        /// <summary>
        /// Собирает конечный тип
        /// </summary>
        /// <returns>Готовый тип</returns>
        public Type Complete()
        {
            // Сборка типа
            var type = _typeBuilder.CreateType();
            // Результат
            return type;
        }
    }
}
