using System;

namespace ZeroLevel.Patterns.DependencyInjection
{
    public sealed class ParameterAttribute : Attribute
    {
        public string Name { get; }
        public Type Type { get; }

        public ParameterAttribute()
        {
            this.Type = null;
            this.Name = string.Empty;
        }

        public ParameterAttribute(Type type)
        {
            this.Type = type;
            this.Name = string.Empty;
        }

        public ParameterAttribute(string parameterName)
        {
            this.Type = null;
            this.Name = parameterName;
        }

        public ParameterAttribute(Type type, string parameterName)
        {
            this.Name = parameterName;
            this.Type = type;
        }
    }
}