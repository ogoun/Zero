using System;

namespace ZeroLevel.DependencyInjection
{
    public sealed class ResolveAttribute : Attribute
    {
        public string ResolveName { get; }
        public Type ContractType { get; }

        public ResolveAttribute()
        {
            ResolveName = string.Empty; ContractType = null!;
        }

        public ResolveAttribute(string resolveName)
        {
            ResolveName = resolveName; ContractType = null!;
        }

        public ResolveAttribute(Type contractType)
        {
            ResolveName = string.Empty; ContractType = contractType;
        }

        public ResolveAttribute(string resolveName, Type contractType)
        {
            ResolveName = resolveName; ContractType = contractType;
        }
    }
}