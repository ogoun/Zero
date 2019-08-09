using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network.SDL
{
    public class InboxType
        : IBinarySerializable
    {
        /// <summary>
        ///  Type name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Type fields (if composite type), top only 
        /// </summary>
        public Dictionary<string, string> Fields { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Name = reader.ReadString();
            this.Fields = reader.ReadDictionary<string, string>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Name);
            writer.WriteDictionary(this.Fields);
        }
    }
}
