using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest.Specifications
{
    public class TitleSpecification :
        BaseSpecification<TestDTO>
    {
        private readonly string _expected;

        public TitleSpecification(string expected)
        {
            _expected = expected;
        }

        public override bool IsSatisfiedBy(TestDTO o)
        {
            return o.Title == _expected;
        }
    }
}
