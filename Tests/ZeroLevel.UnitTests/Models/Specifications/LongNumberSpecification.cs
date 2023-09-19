using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest.Specifications
{
    public class LongNumberSpecification :
        BaseSpecification<TestDTO>
    {
        private readonly long _expected;

        public LongNumberSpecification(long expected)
        {
            _expected = expected;
        }

        public override bool IsSatisfiedBy(TestDTO o)
        {
            return o.LongNumber == _expected;
        }
    }
}
