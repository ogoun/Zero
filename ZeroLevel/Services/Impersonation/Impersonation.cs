using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Класс реализует перевод исполнения программы на права указанного пользователя
    /// </summary>
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public class Impersonation : IDisposable
    {
        WindowsImpersonationContext impersonationContext;

        #region Private methods
        /// <summary>
        /// Назначение текущему процессу прав от указанного процесса, путем копирования его токена
        /// </summary>
        /// <param name="hProcess">Указатель на процесс</param>
        private void ImpersonateByProcess(IntPtr hProcess)
        {
            MySafeTokenHandle token;
            if (!ImpersonationNativeMethods.OpenProcessToken(hProcess, ImpersonationNativeMethods.TokenDesiredAccess.TOKEN_DUPLICATE, out token))
                throw new ApplicationException("Не удалось получить токен процесса.  Win32 код ошибки: " + Marshal.GetLastWin32Error());
            ImpersonateToken(token);
        }
        /// <summary>
        /// Метод назначает текущему процессу дубликат переданного токена
        /// </summary>
        /// <param name="token">Токен</param>
        private void ImpersonateToken(MySafeTokenHandle token)
        {
            MySafeTokenHandle tokenDuplicate;
            WindowsIdentity tempWindowsIdentity;
            using (token)
            {
                if (ImpersonationNativeMethods.DuplicateToken(token, (int)ImpersonationNativeMethods.SecurityImpersonationLevel.SecurityImpersonation, out tokenDuplicate) != 0)
                {
                    using (tokenDuplicate)
                    {
                        if (!tokenDuplicate.IsInvalid)
                        {
                            tempWindowsIdentity = new WindowsIdentity(tokenDuplicate.DangerousGetHandle());
                            impersonationContext = tempWindowsIdentity.Impersonate();
                            return;
                        }
                    }
                }
                else
                    throw new Exception("Не удалось создать дубликат указанного токена. Win32 код ошибки: " + Marshal.GetLastWin32Error());
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Вход от имени указанного пользователя
        /// </summary>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="domain">Домен</param>
        /// <param name="password">Пароль</param>
        /// <returns>false - если не удалось выполнить вход по указанным данным</returns>
        public void ImpersonateByUser(String userName, String domain, String password)
        {
            MySafeTokenHandle token;
            if (ImpersonationNativeMethods.LogonUserA(userName, domain, password, (int)ImpersonationNativeMethods.LogonType.LOGON32_LOGON_INTERACTIVE, (int)ImpersonationNativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT, out token) != 0)
            {
                ImpersonateToken(token);
            }
            else
            {
                throw new Exception("LogonUser failed: " + Marshal.GetLastWin32Error().ToString());
            }
        }
        /// <summary>
        /// Вход от имени указанного пользователя с указанием способа авторизации
        /// </summary>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="domain">Домен</param>
        /// <param name="password">Пароль</param>
        /// <param name="logonType">Тип авторизации</param>
        public void ImpersonateByUser(String userName, String domain, String password, ImpersonationNativeMethods.LogonType logonType)
        {
            MySafeTokenHandle token;
            if (ImpersonationNativeMethods.LogonUserA(userName, domain, password, (int)logonType, (int)ImpersonationNativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT, out token) != 0)
            {
                ImpersonateToken(token);
            }
            else
            {
                throw new Exception("LogonUser failed: " + Marshal.GetLastWin32Error().ToString());
            }
        }
        /// <summary>
        /// Копирование прав указанного процесса
        /// </summary>
        /// <param name="ProcessName">Имя процесса</param>
        public void ImpersonateByProcess(string ProcessName)
        {
            Process[] myProcesses = Process.GetProcesses();
            foreach (Process currentProcess in myProcesses)
            {
                if (currentProcess.ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    ImpersonateByProcess(currentProcess.Handle);
                    break;
                }
            }
        }
        /// <summary>
        /// Копирование прав указанного процесса
        /// </summary>
        /// <param name="ProcessID">Идентификатор процесса</param>
        public void ImpersonateByProcess(int ProcessID)
        {
            Process[] myProcesses = Process.GetProcesses();
            foreach (Process currentProcess in myProcesses)
            {
                if (currentProcess.Id == ProcessID)
                {
                    ImpersonateByProcess(currentProcess.Handle);
                    break;
                }
            }
        }
        #endregion

        /// <summary>
        /// При освобождении рессурсов вернем предыдущего пользователя
        /// </summary>
        public void Dispose()
        {
            impersonationContext?.Undo();
            impersonationContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
