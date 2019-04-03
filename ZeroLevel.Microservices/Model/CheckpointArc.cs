using System.Runtime.Serialization;

namespace ZeroLevel.Microservices.Model
{
    [DataContract]
    public sealed class CheckpointArc
        : Checkpoint
    {
        public CheckpointArc(Checkpoint other)
        {
            this.Id = other.Id;
            this.Id = other.Id;
            this.Timestamp = other.Timestamp;
            this.SourceAppKey = other.SourceAppKey;
            this.DestinationAppKey = other.DestinationAppKey;
            this.CheckpointType = other.CheckpointType;
            this.Payload = other.Payload;
            this.ReasonPhrase = other.ReasonPhrase;
        }
    }
}