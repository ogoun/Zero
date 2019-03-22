using System;
using System.Collections.Generic;

namespace ZeroLevel.Specification
{
    public interface ISpecificationFinder
    {
        IEnumerable<string> Filters { get; }
        Type GetFilterType(string filterName);
        ISpecification<T> GetFilter<T>(string filterName, params object[] args);
    }
}
