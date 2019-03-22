using System;
using System.Collections.Generic;
using ZeroLevel.Specification;

namespace ZeroLevel.Contracts.Specification.Building
{
    public interface ISpecificationBuilder
    {
        string Name { get; }
        Type FilterType { get; }
        IEnumerable<SpecificationParameter> Parameters{get;}
        void ParametersTraversal(Action<SpecificationParameter> parameterHandler);
        ISpecification<T> Build<T>();
        bool Equals(ISpecificationBuilder other);
    }
}
