using System;

namespace ZeroLevel.EventServer.Model
{
    public class PeriodicTimeEvent
        : BaseEvent
    {
        public TimeSpan Period { get; set; }
    }
}
