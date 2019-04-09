using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroLevel.Specification;
using ZeroSpecificationPatternsTest.Models;
using ZeroSpecificationPatternsTest.Specifications;

namespace ZeroSpecificationPatternsTest
{
    /// <summary>
    /// Summary description for SpecificationPatternTest
    /// </summary>
    [TestClass]
    public class SpecificationPatternTest
    {
        [TestMethod]
        public void SimpleSpecificationTest()
        {
            // Assert
            var dto_one = new TestDTO
            {
                IsFlag = true,
                LongNumber = 100,
                Number = 1000,
                Real = 3.141,
                Summary = "Summary",
                Title = "Title"
            };
            var dto_two = new TestDTO
            {
                IsFlag = false,
                LongNumber = 0,
                Number = 0,
                Real = 0,
                Summary = string.Empty,
                Title = string.Empty
            };
            var flag_spec_true = new IsFlagSpecification(true);
            var flag_spec_false = new IsFlagSpecification(false);

            var long_specification_full = new LongNumberSpecification(100);
            var long_specification_empty = new LongNumberSpecification(0);

            var number_specification_full = new NumberSpecification(1000);
            var number_specification_empty = new NumberSpecification(0);

            var real_specification_full = new RealSpecification(3.141);
            var real_specification_empty = new RealSpecification(0);

            var summary_specification_full = new SummarySpecification("Summary");
            var summary_specification_empty = new SummarySpecification(string.Empty);

            var title_specification_full = new TitleSpecification("Title");
            var title_specification_empty = new TitleSpecification(string.Empty);

            // Assert
            Assert.IsTrue(flag_spec_true.IsSatisfiedBy(dto_one));
            Assert.IsFalse(flag_spec_false.IsSatisfiedBy(dto_one));
            Assert.IsTrue(flag_spec_false.IsSatisfiedBy(dto_two));
            Assert.IsFalse(flag_spec_true.IsSatisfiedBy(dto_two));

            Assert.IsTrue(long_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(long_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(long_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(long_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(number_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(number_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(number_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(number_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(real_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(real_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(real_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(real_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(summary_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(summary_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(summary_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(summary_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(title_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(title_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(title_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(title_specification_full.IsSatisfiedBy(dto_two));
        }

        [TestMethod]
        public void NotSpecificationTest()
        {
            // Assert
            var dto_one = new TestDTO
            {
                IsFlag = true,
                LongNumber = 100,
                Number = 1000,
                Real = 3.141,
                Summary = "Summary",
                Title = "Title"
            };
            var dto_two = new TestDTO
            {
                IsFlag = false,
                LongNumber = 0,
                Number = 0,
                Real = 0,
                Summary = string.Empty,
                Title = string.Empty
            };
            var flag_spec_true = new IsFlagSpecification(true).Not();
            var flag_spec_false = new IsFlagSpecification(false).Not();

            var long_specification_full = new LongNumberSpecification(100).Not();
            var long_specification_empty = new LongNumberSpecification(0).Not();

            var number_specification_full = new NumberSpecification(1000).Not();
            var number_specification_empty = new NumberSpecification(0).Not();

            var real_specification_full = new RealSpecification(3.141).Not();
            var real_specification_empty = new RealSpecification(0).Not();

            var summary_specification_full = new SummarySpecification("Summary").Not();
            var summary_specification_empty = new SummarySpecification(string.Empty).Not();

            var title_specification_full = new TitleSpecification("Title").Not();
            var title_specification_empty = new TitleSpecification(string.Empty).Not();

            // Assert
            Assert.IsFalse(flag_spec_true.IsSatisfiedBy(dto_one));
            Assert.IsTrue(flag_spec_false.IsSatisfiedBy(dto_one));
            Assert.IsFalse(flag_spec_false.IsSatisfiedBy(dto_two));
            Assert.IsTrue(flag_spec_true.IsSatisfiedBy(dto_two));

            Assert.IsFalse(long_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsTrue(long_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsFalse(long_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsTrue(long_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsFalse(number_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsTrue(number_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsFalse(number_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsTrue(number_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsFalse(real_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsTrue(real_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsFalse(real_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsTrue(real_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsFalse(summary_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsTrue(summary_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsFalse(summary_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsTrue(summary_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsFalse(title_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsTrue(title_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsFalse(title_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsTrue(title_specification_full.IsSatisfiedBy(dto_two));
        }

        [TestMethod]
        public void ComposedAndSpecificationTest()
        {
            // Assert
            var dto_one = new TestDTO
            {
                IsFlag = true,
                LongNumber = 100,
                Number = 1000,
                Real = 3.141,
                Summary = "Summary",
                Title = "Title"
            };
            var dto_two = new TestDTO
            {
                IsFlag = false,
                LongNumber = 0,
                Number = 0,
                Real = 0,
                Summary = string.Empty,
                Title = string.Empty
            };
            var flag_spec_true = new IsFlagSpecification(true);
            var flag_spec_false = new IsFlagSpecification(false);

            var long_specification_full = new LongNumberSpecification(100);
            var long_specification_empty = new LongNumberSpecification(0);

            var number_specification_full = new NumberSpecification(1000);
            var number_specification_empty = new NumberSpecification(0);

            var real_specification_full = new RealSpecification(3.141);
            var real_specification_empty = new RealSpecification(0);

            var summary_specification_full = new SummarySpecification("Summary");
            var summary_specification_empty = new SummarySpecification(string.Empty);

            var title_specification_full = new TitleSpecification("Title");
            var title_specification_empty = new TitleSpecification(string.Empty);

            // Act

            var composed_full = flag_spec_true.
                And(long_specification_full).
                And(number_specification_full).
                And(real_specification_full).
                And(summary_specification_full).
                And(title_specification_full);

            var composed_empty = flag_spec_false.
                And(long_specification_empty).
                And(number_specification_empty).
                And(real_specification_empty).
                And(summary_specification_empty).
                And(title_specification_empty);

            // Assert
            Assert.IsTrue(composed_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(composed_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(composed_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(composed_full.IsSatisfiedBy(dto_two));
        }

        [TestMethod]
        public void ComposedOrSpecificationTest()
        {
            // Assert
            var dto_one = new TestDTO
            {
                IsFlag = true,
                LongNumber = 100,
                Number = 1000,
                Real = 3.141,
                Summary = "Summary",
                Title = "Title"
            };
            var dto_two = new TestDTO
            {
                IsFlag = false,
                LongNumber = 0,
                Number = 0,
                Real = 0,
                Summary = string.Empty,
                Title = string.Empty
            };
            var flag_spec_true = new IsFlagSpecification(true);
            var flag_spec_false = new IsFlagSpecification(false);

            var long_specification_full = new LongNumberSpecification(100);
            var long_specification_empty = new LongNumberSpecification(0);

            var number_specification_full = new NumberSpecification(1000);
            var number_specification_empty = new NumberSpecification(0);

            var real_specification_full = new RealSpecification(3.141);
            var real_specification_empty = new RealSpecification(0);

            var summary_specification_full = new SummarySpecification("Summary");
            var summary_specification_empty = new SummarySpecification(string.Empty);

            var title_specification_full = new TitleSpecification("Title");
            var title_specification_empty = new TitleSpecification(string.Empty);

            // Act

            var composed_full = flag_spec_false.
                Or(long_specification_full).
                Or(number_specification_full).
                Or(real_specification_full).
                Or(summary_specification_full).
                Or(title_specification_full);

            var composed_empty = flag_spec_true.
                Or(long_specification_empty).
                Or(number_specification_empty).
                Or(real_specification_empty).
                Or(summary_specification_empty).
                Or(title_specification_empty);

            // Assert
            Assert.IsTrue(composed_full.IsSatisfiedBy(dto_one));
            Assert.IsTrue(composed_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(composed_empty.IsSatisfiedBy(dto_two));
            Assert.IsTrue(composed_full.IsSatisfiedBy(dto_two));
        }

        [TestMethod]
        public void ExpressionSpecificationTest()
        {
            // Assert
            var dto_one = new TestDTO
            {
                IsFlag = true,
                LongNumber = 100,
                Number = 1000,
                Real = 3.141,
                Summary = "Summary",
                Title = "Title"
            };
            var dto_two = new TestDTO
            {
                IsFlag = false,
                LongNumber = 0,
                Number = 0,
                Real = 0,
                Summary = string.Empty,
                Title = string.Empty
            };
            var flag_spec_true = new ExpressionSpecification<TestDTO>(o=>o.IsFlag == true);
            var flag_spec_false = new ExpressionSpecification<TestDTO>(o => o.IsFlag == false);

            var long_specification_full = new ExpressionSpecification<TestDTO>(o => o.LongNumber == 100);
            var long_specification_empty = new ExpressionSpecification<TestDTO>(o => o.LongNumber == 0);

            var number_specification_full = new ExpressionSpecification<TestDTO>(o => o.Number == 1000);
            var number_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Number == 0);

            var real_specification_full = new ExpressionSpecification<TestDTO>(o => o.Real == 3.141);
            var real_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Real == 0);

            var summary_specification_full = new ExpressionSpecification<TestDTO>(o => o.Summary == "Summary");
            var summary_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Summary == string.Empty);

            var title_specification_full = new ExpressionSpecification<TestDTO>(o => o.Title == "Title");
            var title_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Title == string.Empty);

            // Assert
            Assert.IsTrue(flag_spec_true.IsSatisfiedBy(dto_one));
            Assert.IsFalse(flag_spec_false.IsSatisfiedBy(dto_one));
            Assert.IsTrue(flag_spec_false.IsSatisfiedBy(dto_two));
            Assert.IsFalse(flag_spec_true.IsSatisfiedBy(dto_two));

            Assert.IsTrue(long_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(long_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(long_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(long_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(number_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(number_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(number_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(number_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(real_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(real_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(real_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(real_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(summary_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(summary_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(summary_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(summary_specification_full.IsSatisfiedBy(dto_two));

            Assert.IsTrue(title_specification_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(title_specification_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(title_specification_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(title_specification_full.IsSatisfiedBy(dto_two));
        }

        [TestMethod]
        public void ComposedExpressionSpecificationTest()
        {
            // Assert
            var dto_one = new TestDTO
            {
                IsFlag = true,
                LongNumber = 100,
                Number = 1000,
                Real = 3.141,
                Summary = "Summary",
                Title = "Title"
            };
            var dto_two = new TestDTO
            {
                IsFlag = false,
                LongNumber = 0,
                Number = 0,
                Real = 0,
                Summary = string.Empty,
                Title = string.Empty
            };
            var flag_spec_true = new ExpressionSpecification<TestDTO>(o => o.IsFlag == true);
            var flag_spec_false = new ExpressionSpecification<TestDTO>(o => o.IsFlag == false);

            var long_specification_full = new ExpressionSpecification<TestDTO>(o => o.LongNumber == 100);
            var long_specification_empty = new ExpressionSpecification<TestDTO>(o => o.LongNumber == 0);

            var number_specification_full = new ExpressionSpecification<TestDTO>(o => o.Number == 1000);
            var number_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Number == 0);

            var real_specification_full = new ExpressionSpecification<TestDTO>(o => o.Real == 3.141);
            var real_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Real == 0);

            var summary_specification_full = new ExpressionSpecification<TestDTO>(o => o.Summary == "Summary");
            var summary_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Summary == string.Empty);

            var title_specification_full = new ExpressionSpecification<TestDTO>(o => o.Title == "Title");
            var title_specification_empty = new ExpressionSpecification<TestDTO>(o => o.Title == string.Empty);

            // Act

            var composed_full = flag_spec_true.
                And(long_specification_full).
                And(number_specification_full).
                And(real_specification_full).
                And(summary_specification_full).
                And(title_specification_full);

            var composed_empty = flag_spec_false.
                And(long_specification_empty).
                And(number_specification_empty).
                And(real_specification_empty).
                And(summary_specification_empty).
                And(title_specification_empty);

            // Assert
            Assert.IsTrue(composed_full.IsSatisfiedBy(dto_one));
            Assert.IsFalse(composed_empty.IsSatisfiedBy(dto_one));
            Assert.IsTrue(composed_empty.IsSatisfiedBy(dto_two));
            Assert.IsFalse(composed_full.IsSatisfiedBy(dto_two));
        }
    }
}
