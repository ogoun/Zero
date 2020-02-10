namespace ZeroLevel.EventServer.Model
{
    public abstract class BaseEvent
    {
        public string ServiceKey { get; set; }
        public string Inbox { get; set; }
    }
}
