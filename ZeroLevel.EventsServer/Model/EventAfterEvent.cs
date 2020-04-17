namespace ZeroLevel.EventServer.Model
{
    public class EventAfterEvent
        : BaseEvent
    {
        public long EventId { get; set; }

        public Condition Confition { get; set; }
    }
}
