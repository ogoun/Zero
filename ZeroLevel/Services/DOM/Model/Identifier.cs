using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public class Identifier : 
        IBinarySerializable
    {
        public Identifier() { }
        public Identifier(IBinaryReader reader) { Deserialize(reader); }

        #region Fields        
        /// <summary>
        /// Версия документа
        /// </summary>
        public int Version;
        /// <summary>
        /// Идентификатор по дате выхода, дает возможность идентифицировать 
        /// последнюю полученную по запросу новость, для последующих запросов
        /// обновлений
        /// </summary>
        public string Timestamp;
        /// <summary>
        /// Идентификатор по дате выхода с масштабированием до дня (20161024)
        /// </summary>
        public string DateLabel;
        #endregion

        #region IBinarySerializable
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.Version);
            writer.WriteString(this.Timestamp);
            writer.WriteString(this.DateLabel);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Version = reader.ReadInt32();
            this.Timestamp = reader.ReadString();
            this.DateLabel = reader.ReadString();
        }
        #endregion
    }
}
