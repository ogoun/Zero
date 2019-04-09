using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest.Specifications
{
    public class SummarySpecification :
        BaseSpecification<TestDTO>
    {
        private readonly string _expected;

        public SummarySpecification(string expected)
        {
            _expected = expected;
        }

        public override bool IsSatisfiedBy(TestDTO o)
        {
            return o.Summary == _expected;
        }
    }
}
