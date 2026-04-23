using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Models
{
    public sealed class DataRequest
        : IBinarySerializable
    {
        public string Symbol { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public string? Data { get; set; } = null!;

        public void Deserialize(IBinaryReader reader)
        {
            this.Symbol = reader.ReadString();
            this.Start = reader.ReadDateTimeOffset();
            this.End = reader.ReadDateTimeOffset();
            this.Data = reader.ReadString();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Symbol);
            writer.WriteDateTimeOffset(this.Start);
            writer.WriteDateTimeOffset(this.End);
            writer.WriteString(this.Data);
        }
    }
}
