﻿using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public class Identifier :
        IBinarySerializable
    {
        public Identifier()
        {
        }

        public Identifier(IBinaryReader reader)
        {
            Deserialize(reader);
        }

        #region Fields

        /// <summary>
        /// Version
        /// </summary>
        public int Version;

        /// <summary>
        /// Timestamp ID
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// Label with day accurcy
        /// </summary>
        public string DateLabel;

        /// <summary>
        /// Document unique link
        /// </summary>
        public string Link;

        #endregion Fields

        #region IBinarySerializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.Version);
            writer.WriteLong(this.Timestamp);
            writer.WriteString(this.DateLabel);
            writer.WriteString(this.Link);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Version = reader.ReadInt32();
            this.Timestamp = reader.ReadLong();
            this.DateLabel = reader.ReadString();
            this.Link = reader.ReadString();
        }

        #endregion IBinarySerializable
    }
}