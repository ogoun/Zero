using System;

namespace ZeroLevel.Sleopok.Engine.Models
{
    public enum SleoFieldType
    {
        /// <summary>
        /// One value
        /// </summary>
        Single = 0,
        /// <summary>
        /// Array of values
        /// </summary>
        Array = 1,
    }
    internal sealed class SleoField
    {
        internal SleoField(SleoFieldType fieldType, string name, float boost, bool exactMatch, Func<object, object> getter) =>
            (FieldType, Name, Boost, ExactMatch, Getter) = (fieldType, name, boost, exactMatch, getter);

        public SleoFieldType FieldType;
        public string Name;
        public float Boost;
        public bool ExactMatch;
        public Func<object, object> Getter;
    }
}
