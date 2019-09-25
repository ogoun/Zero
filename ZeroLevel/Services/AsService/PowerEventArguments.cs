namespace ZeroLevel.Services.AsService
{
    public enum PowerEventCode
    {
        /// <summary>
        /// The system has requested permission to suspend the computer. An application that grants permission should carry out preparations for the suspension before returning.
        /// <remarks>Not supported by <see cref="ConsoleRunHost"/></remarks>
        /// </summary>
        QuerySuspend = 0,
        /// <summary>
        /// The system was denied permission to suspend the computer. This status is broadcast if any application or driver denied a previous <see cref="QuerySuspend"/> status.
        /// <remarks>Not supported by <see cref="ConsoleRunHost"/></remarks>
        /// </summary>
        QuerySuspendFailed = 2,
        /// <summary>
        /// The computer is about to enter a suspended state. This event is typically broadcast when all applications and installable drivers have returned true to a previous QuerySuspend state.
        /// </summary>
        Suspend = 4,
        /// <summary>
        /// The system has resumed operation after a critical suspension caused by a failing battery.
        /// <remarks>Not supported by <see cref="ConsoleRunHost"/></remarks>
        /// </summary>
        ResumeCritical = 6,
        /// <summary>
        /// The system has resumed operation after being suspended.
        /// <remarks>Not supported by <see cref="ConsoleRunHost"/></remarks>
        /// </summary>
        ResumeSuspend = 7,
        /// <summary>
        /// Battery power is low.
        /// <remarks>Not supported by <see cref="ConsoleRunHost"/></remarks>
        /// </summary>
        BatteryLow = 9,
        /// <summary>
        /// A change in the power status of the computer is detected, such as a switch from battery power to A/C. The system also broadcasts this event when remaining battery power slips below the threshold specified by the user or if the battery power changes by a specified percentage.
        /// </summary>
        PowerStatusChange = 10,
        /// <summary>
        /// An Advanced Power Management (APM) BIOS signaled an APM OEM event.
        /// <remarks>Not supported by <see cref="ConsoleRunHost"/></remarks>
        /// </summary>
        OemEvent = 11,
        /// <summary>
        /// The computer has woken up automatically to handle an event.
        /// </summary>
        ResumeAutomatic = 18
    }

    public interface PowerEventArguments
    {
        PowerEventCode EventCode { get; }
    }
}
