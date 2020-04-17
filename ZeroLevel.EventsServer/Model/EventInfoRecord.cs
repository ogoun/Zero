namespace ZeroLevel.EventServer
{
    public class EventInfoRecord
    {
        public long EventId { get; set; }

        public string ServiceKey { get; set; }
        // OR
        public string ServiceEndpoint { get; set; }

        public string Inbox { get; set; }
    }
}
