using System;
using System.Runtime.Serialization;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    [Serializable]
    [DataContract]
    public sealed class ZeroServiceInfo :
       IEquatable<ZeroServiceInfo>, IBinarySerializable
    {
        public const string DEFAULT_GROUP_NAME = "__service_default_group__";
        public const string DEFAULT_TYPE_NAME = "__service_default_type__";

        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Service key, must be unique within the business functionality.
        /// two services with same key will be horizontally balanced
        /// </summary>
        [DataMember]
        public string ServiceKey { get; set; }

        /// <summary>
        /// The group can determine the services working in the same domain
        /// </summary>
        [DataMember]
        public string ServiceGroup { get; set; } = DEFAULT_GROUP_NAME;

        /// <summary>
        /// The type of service, for filtering, determines membership in a subgroup.
        /// </summary>
        [DataMember]
        public string ServiceType { get; set; } = DEFAULT_TYPE_NAME;
        /// <summary>
        /// Service version
        /// </summary>
        [DataMember]
        public string Version { get; set; }


        public bool Equals(ZeroServiceInfo other)
        {
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (string.Compare(this.Name, other.Name, true) != 0) return false;
            if (string.Compare(this.ServiceKey, other.ServiceKey, true) != 0) return false;
            if (string.Compare(this.ServiceGroup, other.ServiceGroup, true) != 0) return false;
            if (string.Compare(this.ServiceType, other.ServiceType, true) != 0) return false;
            if (string.Compare(this.Version, other.Version, true) != 0) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ZeroServiceInfo);
        }

        public override int GetHashCode()
        {
            return this.ServiceKey.GetHashCode();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Name);
            writer.WriteString(this.ServiceKey);
            writer.WriteString(this.ServiceGroup);
            writer.WriteString(this.ServiceType);
            writer.WriteString(this.Version);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Name = reader.ReadString();
            this.ServiceKey = reader.ReadString();
            this.ServiceGroup = reader.ReadString();
            this.ServiceType = reader.ReadString();
            this.Version = reader.ReadString();
        }

        public override string ToString()
        {
            return $"{ServiceKey } ({Version})";
        }
    }
}