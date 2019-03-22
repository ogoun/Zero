using System;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Реализует исполнение произвольного кода от прав указанного процесса
    /// </summary>
    public class ProcessImpersonationExecutor 
        : IImpersonationExecutor
    {
        private string _processName = string.Empty;
        private int _pid = -1;

        public ProcessImpersonationExecutor(string processName)
        {
            _processName = processName;
        }

        public ProcessImpersonationExecutor(int pid)
        {
            _pid = pid;
        }
        /// <summary>
        /// Исполнение кода
        /// </summary>
        /// <typeparam name="T">Тип передаваемого аргумента</typeparam>
        /// <param name="action">Делегат</param>
        /// <param name="arg">Аргумент</param>
        public void ExecuteCode<T>(Action<T> action, T arg)
        {
            using (Impersonation imp = new Impersonation())
            {
                if (!String.IsNullOrWhiteSpace(_processName))
                {
                    imp.ImpersonateByProcess(_processName);
                }
                else if (_pid > -1)
                {
                    imp.ImpersonateByProcess(_pid);
                }
                else
                {
                    throw new Exception("Нет данных для идентификации процесса. Для копирования прав процесса требуется указать его имя или идентификатор.");
                }
                action(arg);
            }
        }
        /// <summary>
        /// Исполнение кода
        /// </summary>
        /// <typeparam name="T">Тип передаваемого аргумента</typeparam>
        /// <param name="action">Делегат</param>
        public void ExecuteCode(Action action)
        {
            using (Impersonation imp = new Impersonation())
            {
                if (!String.IsNullOrWhiteSpace(_processName))
                {
                    imp.ImpersonateByProcess(_processName);
                }
                else if (_pid > -1)
                {
                    imp.ImpersonateByProcess(_pid);
                }
                else
                {
                    throw new Exception("Нет данных для идентификации процесса. Для копирования прав процесса требуется указать его имя или идентификатор.");
                }
                action();
            }
        }
    }
}
