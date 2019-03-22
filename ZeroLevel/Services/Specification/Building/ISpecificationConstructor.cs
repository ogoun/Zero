using System.Collections.Generic;

namespace ZeroLevel.Contracts.Specification.Building
{
    public interface ISpecificationConstructor
    {
        string Name { get; }
        IEnumerable<string> VariantNames { get; }
        ISpecificationBuilder GetVariant(string variantName);
    }
}
