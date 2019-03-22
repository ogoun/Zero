using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    public interface IContainer :
        IContainerRegister,
        IContainerInstanceRegister,
        IResolver,
        ICompositionProvider,
        IParameterStorage,
        IDisposable
    {
        bool IsResolvingExists<T>();
        bool IsResolvingExists<T>(string resolveName);
        bool IsResolvingExists(Type type);
        bool IsResolvingExists(Type type, string resolveName);
    }
}
