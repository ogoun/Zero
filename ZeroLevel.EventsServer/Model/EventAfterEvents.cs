using System.Collections.Generic;

namespace ZeroLevel.EventServer.Model
{
    public class EventAfterEvents
        : BaseEvent
    {
        public IEnumerable<long> EventIds { get; set; }

        public Condition Confition { get; set; }
    }
}
