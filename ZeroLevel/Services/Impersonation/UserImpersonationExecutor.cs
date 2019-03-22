using System;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Класс исполняющий произвольный код с правами указанного пользователя
    /// </summary>
    public class UserImpersonationExecutor 
        : IImpersonationExecutor
    {
        private string USR { get; set; }
        private string DOMAIN { get; set; }
        private string PWD { get; set; }

        private ImpersonationNativeMethods.LogonType logonType = ImpersonationNativeMethods.LogonType.LOGON32_LOGON_INTERACTIVE;

        public UserImpersonationExecutor(string userName, string domainName, string password)
        {
            USR = userName;
            DOMAIN = domainName;
            PWD = password;
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
                imp.ImpersonateByUser(USR, DOMAIN, PWD, logonType);
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
                imp.ImpersonateByUser(USR, DOMAIN, PWD, logonType);
                action();
            }
        }
    }
}
