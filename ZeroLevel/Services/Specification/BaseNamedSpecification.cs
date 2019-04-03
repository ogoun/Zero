using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Specification
{
    [Serializable]
    [DataContract]
    public abstract class BaseNamedSpecification<T> : BaseSpecification<T>
    {
        [DataMember]
        private string _name;

        [DataMember]
        private string _description;

        public string Name { get { return _name; } }
        public string Description { get { return _description; } }

        public BaseNamedSpecification(string name, string description)
        {
            _name = name;
            _description = description;
        }
    }
}