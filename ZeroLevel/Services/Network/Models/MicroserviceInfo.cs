using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Network.Microservices
{
    [Serializable]
    [DataContract]
    public sealed class MicroserviceInfo :
       IEquatable<MicroserviceInfo>
    {
        public const string DEFAULT_GROUP_NAME = "__service_default_group__";
        public const string DEFAULT_TYPE_NAME = "__service_default_type__";

        /// <summary>
        /// Ключ сервиса, должен быть уникален в рамках бизнес функционала
        /// т.е. с одинаковым ключом могут работать только копии сервиса, для горизонтальной балансировки
        /// </summary>
        [DataMember]
        public string ServiceKey { get; set; }
        /// <summary>
        /// Группа, для фильтрации, в качетсве группы можно определять сервисы работающие в одном домене,
        /// например сервисы обрабатывющие новости в одной группе, сервисы по котировкам в другой
        /// </summary>
        [DataMember]
        public string ServiceGroup { get; set; } = DEFAULT_GROUP_NAME;
        /// <summary>
        /// Тип сервиса, для фильтрации, определяет принадлежность к подгруппе, например сервисы для доставки информации,
        /// или сервисы-адаптеры и т.д.
        /// </summary>
        [DataMember]
        public string ServiceType { get; set; } = DEFAULT_TYPE_NAME;
        /// <summary>
        /// Протокол по которому разрешен доступ к API сервиса
        /// </summary>
        [DataMember]
        public string Protocol { get; set; }
        /// <summary>
        /// Точка подключения, адрес
        /// </summary>
        [DataMember]
        public string Endpoint { get; set; }
        /// <summary>
        /// Версия сервиса
        /// </summary>
        [DataMember]
        public string Version { get; set; }

        public bool Equals(MicroserviceInfo other)
        {
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (string.Compare(this.ServiceKey, other.ServiceKey, true) != 0) return false;
            if (string.Compare(this.ServiceGroup, other.ServiceGroup, true) != 0) return false;
            if (string.Compare(this.ServiceType, other.ServiceType, true) != 0) return false;

            if (string.Compare(this.Endpoint, other.Endpoint, true) != 0) return false;
            if (string.Compare(this.Version, other.Version, true) != 0) return false;
            if (string.Compare(this.Protocol, other.Protocol, true) != 0) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.ServiceKey.GetHashCode() ^ this.Protocol.GetHashCode() ^ this.Endpoint.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ServiceKey} ({Version})";
        }
    }
}
