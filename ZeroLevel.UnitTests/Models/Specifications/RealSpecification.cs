using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest.Specifications
{
    public class RealSpecification :
        BaseSpecification<TestDTO>
    {
        private readonly double _expected;

        public RealSpecification(double expected)
        {
            _expected = expected;
        }

        public override bool IsSatisfiedBy(TestDTO o)
        {
            return o.Real == _expected;
        }
    }
}
