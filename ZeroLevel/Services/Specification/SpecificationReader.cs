using System;
using System.Reflection;
using ZeroLevel.Contracts.Specification.Building;
using ZeroLevel.Specification;

namespace ZeroLevel.Services
{
    public static class SpecificationReader
    {
        public static ISpecificationFinder GetAssemblySpecificationFinder(Assembly asm)
        {
            return new AssemblySpecificationFactory(asm);
        }

        public static ISpecificationConstructor GetSpecificationConstructor(Type specificationType)
        {
            return new SpecificationConstructor(specificationType);
        }
    }
}