using System;

namespace ZeroLevel.Sleopok.Engine.Models
{
    public sealed class SleoIndexAttribute
        : Attribute
    {
        public readonly string Name;
        public readonly float Boost;
        public readonly bool AvaliableForExactMatch;
        
        public SleoIndexAttribute(string name, float boost = 1.0f, bool avaliableForExactMatch = false)
        {
            Name = name;
            Boost = boost;
            AvaliableForExactMatch = avaliableForExactMatch;
        }
    }
}
