using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest.Specifications
{
    public class IsFlagSpecification :
        BaseSpecification<TestDTO>
    {
        private readonly bool _expected;

        public IsFlagSpecification(bool expected)
        {
            _expected = expected;
        }

        public override bool IsSatisfiedBy(TestDTO o)
        {
            return o.IsFlag == _expected;
        }
    }
}
