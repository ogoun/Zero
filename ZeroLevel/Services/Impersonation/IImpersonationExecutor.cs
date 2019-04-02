using System;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Interface for classes executing code from user or process rights
    /// </summary>
    public interface IImpersonationExecutor
    {
        void ExecuteCode<T>(Action<T> action, T arg);
        void ExecuteCode(Action action);
    }
}
