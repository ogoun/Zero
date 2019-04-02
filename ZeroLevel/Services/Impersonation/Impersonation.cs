using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// The class implements the translation of the program execution to the rights of the specified user.
    /// </summary>
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public class Impersonation : IDisposable
    {
        WindowsImpersonationContext impersonationContext;

        #region Private methods
        /// <summary>
        /// Assigning rights to the current process from the specified process, by copying its token
        /// </summary>
        /// <param name="hProcess">Process pointer</param>
        private void ImpersonateByProcess(IntPtr hProcess)
        {
            MySafeTokenHandle token;
            if (!ImpersonationNativeMethods.OpenProcessToken(hProcess, ImpersonationNativeMethods.TokenDesiredAccess.TOKEN_DUPLICATE, out token))
                throw new ApplicationException("Failed to get the process token. Win32 error code: " + Marshal.GetLastWin32Error());
            ImpersonateToken(token);
        }
        /// <summary>
        /// The method assigns a duplicate token to the current process.
        /// </summary>
        /// <param name="token">Token</param>
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
                    throw new Exception("Failed to create a duplicate of the specified token. Win32 error code: " + Marshal.GetLastWin32Error());
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Login as a specified user
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="domain">User domain</param>
        /// <param name="password">User password</param>
        /// <returns>false - if failed to log in with the specified data</returns>
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
        /// Copying the rights of the specified process
        /// </summary>
        /// <param name="ProcessID">Process id</param>
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
        /// When releasing resources, we will return the previous user right
        /// </summary>
        public void Dispose()
        {
            impersonationContext?.Undo();
            impersonationContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
