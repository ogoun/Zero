using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class Assotiation : 
        IBinarySerializable
    {
        #region Fields
        /// <summary>
        /// Заголовок
        /// </summary>
        public string Title;
        /// <summary>
        /// Описание (например, что было изменено по сравнению с прошлой версией)
        /// </summary>
        public string Description;
        /// <summary>
        /// Ссылка на связанный документ
        /// </summary>
        public Guid DocumentId;
        /// <summary>
        /// Тип связи
        /// </summary>
        public AssotiationRelation Relation;
        #endregion

        #region IBinarySerializable
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Title);
            writer.WriteString(this.Description);
            writer.WriteGuid(this.DocumentId);
            writer.WriteInt32((Int32)this.Relation);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Title = reader.ReadString();
            this.Description = reader.ReadString();
            this.DocumentId = reader.ReadGuid();
            this.Relation = (AssotiationRelation)reader.ReadInt32();
        }
        #endregion
    }
}
