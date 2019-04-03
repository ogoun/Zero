using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class Assotiation :
        IBinarySerializable
    {
        #region Fields

        /// <summary>
        /// Title
        /// </summary>
        public string Title;

        /// <summary>
        /// Description
        /// </summary>
        public string Description;

        /// <summary>
        /// Binded document reference
        /// </summary>
        public Guid DocumentId;

        /// <summary>
        /// Relation type
        /// </summary>
        public AssotiationRelation Relation;

        #endregion Fields

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

        #endregion IBinarySerializable
    }
}