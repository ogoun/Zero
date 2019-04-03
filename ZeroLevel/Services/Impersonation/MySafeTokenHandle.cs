using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;

namespace ZeroLevel.Services.Impersonation
{
    /// <summary>
    /// Implementing a safe pointer
    /// </summary>
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    internal class MySafeTokenHandle
        : SafeHandleZeroOrMinusOneIsInvalid
    {
        private MySafeTokenHandle()
            : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            return ImpersonationNativeMethods.CloseHandle(handle);
        }
    }
}