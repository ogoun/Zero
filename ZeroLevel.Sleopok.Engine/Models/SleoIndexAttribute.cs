using System;

namespace ZeroLevel.Sleopok.Engine.Models
{
    public sealed class SleoIndexAttribute
        : Attribute
    {
        public string Name { get; private set; }
        public float Boost { get; private set; } = 1.0f;
        public bool AvaliableForExactMatch {  get; private set; } = false;
        
        public SleoIndexAttribute(string name, float boost = 1.0f, bool avaliableForExactMatch = false)
        {
            Name = name;
            Boost = boost;
            AvaliableForExactMatch = avaliableForExactMatch;
        }
    }
}
