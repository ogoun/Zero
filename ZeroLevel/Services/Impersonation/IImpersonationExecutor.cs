using System;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Интерфейс для классов исполняющих произвольный код от прав пользователя или процесса
    /// </summary>
    public interface IImpersonationExecutor
    {
        void ExecuteCode<T>(Action<T> action, T arg);
        void ExecuteCode(Action action);
    }
}
