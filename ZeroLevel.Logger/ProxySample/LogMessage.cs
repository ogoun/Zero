using ZeroLevel.Logging;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Logger.ProxySample
{
    public class LogMessage
        : IBinarySerializable
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Level = (LogLevel)reader.ReadInt32();
            this.Message = reader.ReadString();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32((int)this.Level);
            writer.WriteString(this.Message);
        }
    }
}
