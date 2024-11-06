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
        public SleoFieldType FieldType;
        public string Name;
        public float Boost;
        public bool ExactMatch;
        public Func<object, object> Getter;
    }
}
