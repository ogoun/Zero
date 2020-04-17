using System;

namespace ZeroLevel.EventServer.Model
{
    public class OneTimeEvent
        : BaseEvent
    {
        public TimeSpan Period { get; set; }
    }
}
