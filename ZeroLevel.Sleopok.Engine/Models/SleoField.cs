using System;

namespace ZeroLevel.Sleopok.Engine.Models
{
    internal sealed class SleoField
    {
        public string Name;
        public float Boost;
        public bool ExactMatch;
        public Func<object, object> Getter;
    }
}
