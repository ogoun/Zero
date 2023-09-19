using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest.Specifications
{
    public class NumberSpecification :
        BaseSpecification<TestDTO>
    {
        private readonly int _expected;

        public NumberSpecification(int expected)
        {
            _expected = expected;
        }

        public override bool IsSatisfiedBy(TestDTO o)
        {
            return o.Number == _expected;
        }
    }
}
