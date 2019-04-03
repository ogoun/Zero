using System;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Implements the execution of an code from the rights of the specified process
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
        /// Code execution
        /// </summary>
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
                    throw new Exception("No data to identify the process. To copy the rights of a process, you must specify its name or identifier");
                }
                action(arg);
            }
        }

        /// <summary>
        /// Code execution
        /// </summary>
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
                    throw new Exception("No data to identify the process. To copy the rights of a process, you must specify its name or identifier");
                }
                action();
            }
        }
    }
}