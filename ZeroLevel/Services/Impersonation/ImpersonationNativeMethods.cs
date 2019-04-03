using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace ZeroLevel.Services.Impersonation
{
    [SuppressUnmanagedCodeSecurity()]
    public static class ImpersonationNativeMethods
    {
        #region P/Invoke enums

        /// <summary>
        /// Authorization provider
        /// </summary>
        public enum LogonProvider : int
        {
            /// <summary>
            /// Use the standard logon provider for the system.
            /// The default security provider is negotiate, unless you pass NULL for the domain name and the user name
            /// is not in UPN format. In this case, the default provider is NTLM.
            /// NOTE: Windows 2000/NT:   The default security provider is NTLM.
            /// </summary>
            LOGON32_PROVIDER_DEFAULT = 0,
        }

        /// <summary>
        /// Authorization method
        /// </summary>
        public enum LogonType : int
        {
            /// <summary>
            /// This logon type is intended for users who will be interactively using the computer, such as a user being logged on
            /// by a terminal server, remote shell, or similar process.
            /// This logon type has the additional expense of caching logon information for disconnected operations;
            /// therefore, it is inappropriate for some client/server applications,
            /// such as a mail server.
            /// </summary>
            LOGON32_LOGON_INTERACTIVE = 2,

            /// <summary>
            /// This logon type is intended for high performance servers to authenticate plaintext passwords.
            /// The LogonUser function does not cache credentials for this logon type.
            /// </summary>
            LOGON32_LOGON_NETWORK = 3,

            /// <summary>
            /// This logon type is intended for batch servers, where processes may be executing on behalf of a user without
            /// their direct intervention. This type is also for higher performance servers that process many plaintext
            /// authentication attempts at a time, such as mail or Web servers.
            /// The LogonUser function does not cache credentials for this logon type.
            /// </summary>
            LOGON32_LOGON_BATCH = 4,

            /// <summary>
            /// Indicates a service-type logon. The account provided must have the service privilege enabled.
            /// </summary>
            LOGON32_LOGON_SERVICE = 5,

            /// <summary>
            /// This logon type is for GINA DLLs that log on users who will be interactively using the computer.
            /// This logon type can generate a unique audit record that shows when the workstation was unlocked.
            /// </summary>
            LOGON32_LOGON_UNLOCK = 7,

            /// <summary>
            /// This logon type preserves the name and password in the authentication package, which allows the server to make
            /// connections to other network servers while impersonating the client. A server can accept plaintext credentials
            /// from a client, call LogonUser, verify that the user can access the system across the network, and still
            /// communicate with other servers.
            /// NOTE: Windows NT:  This value is not supported.
            /// </summary>
            LOGON32_LOGON_NETWORK_CLEARTEXT = 8,

            /// <summary>
            /// This logon type allows the caller to clone its current token and specify new credentials for outbound connections.
            /// The new logon session has the same local identifier but uses different credentials for other network connections.
            /// NOTE: This logon type is supported only by the LOGON32_PROVIDER_WINNT50 logon provider.
            /// NOTE: Windows NT:  This value is not supported.
            /// </summary>
            LOGON32_LOGON_NEW_CREDENTIALS = 9,
        }

        /// <summary>
        /// Desired access level to token
        /// </summary>
        public struct TokenDesiredAccess
        {
            public static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public static uint STANDARD_RIGHTS_READ = 0x00020000;
            public static uint TOKEN_ASSIGN_PRIMARY = 0x0001;

            /// <summary>
            /// Allows to create a copy
            /// </summary>
            public static uint TOKEN_DUPLICATE = 0x0002;

            public static uint TOKEN_IMPERSONATE = 0x0004;
            public static uint TOKEN_QUERY = 0x0008;
            public static uint TOKEN_QUERY_SOURCE = 0x0010;
            public static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
            public static uint TOKEN_ADJUST_GROUPS = 0x0040;
            public static uint TOKEN_ADJUST_DEFAULT = 0x0080;
            public static uint TOKEN_ADJUST_SESSIONID = 0x0100;
            public static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

            public static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                TOKEN_ADJUST_SESSIONID);
        }

        /// <summary>
        /// The security type is used during the token duplication operation (in the current task)
        /// </summary>
        public enum SecurityImpersonationLevel : int
        {
            /// <summary>
            /// The server process cannot obtain identification information about the client,
            /// and it cannot impersonate the client. It is defined with no value given, and thus,
            /// by ANSI C rules, defaults to a value of zero.
            /// </summary>
            SecurityAnonymous = 0,

            /// <summary>
            /// The server process can obtain information about the client, such as security identifiers and privileges,
            /// but it cannot impersonate the client. This is useful for servers that export their own objects,
            /// for example, database products that export tables and views.
            /// Using the retrieved client-security information, the server can make access-validation decisions without
            /// being able to use other services that are using the client's security context.
            /// </summary>
            SecurityIdentification = 1,

            /// <summary>
            /// The server process can impersonate the client's security context on its local system.
            /// The server cannot impersonate the client on remote systems.
            /// </summary>
            SecurityImpersonation = 2,

            /// <summary>
            /// The server process can impersonate the client's security context on remote systems.
            /// NOTE: Windows NT:  This impersonation level is not supported.
            /// </summary>
            SecurityDelegation = 3,
        }

        #endregion P/Invoke enums

        #region P/Invoke

        /// <summary>
        /// Authorization on behalf of the specified user
        /// </summary>
        /// <param name="lpszUserName">Username</param>
        /// <param name="lpszDomain">Domain</param>
        /// <param name="lpszPassword">Password</param>
        /// <param name="dwLogonType">Authorization Type</param>
        /// <param name="dwLogonProvider">Provider (always 0)</param>
        /// <param name="phToken">Token - login result</param>
        /// <returns></returns>
        [DllImport("advapi32.dll")]
        internal static extern int LogonUserA(String lpszUserName,
            String lpszDomain,
            String lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out MySafeTokenHandle phToken);

        /// <summary>
        /// Creating a duplicate token
        /// </summary>
        /// <param name="hToken">Original token</param>
        /// <param name="impersonationLevel"></param>
        /// <param name="hNewToken"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int DuplicateToken(MySafeTokenHandle hToken,
            int impersonationLevel,
            out MySafeTokenHandle hNewToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Attempt to get a token running process
        /// </summary>
        /// <param name="ProcessHandle">Process pointer</param>
        /// <param name="DesiredAccess"></param>
        /// <param name="TokenHandle">Token - result</param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out MySafeTokenHandle TokenHandle);

        #endregion P/Invoke
    }
}