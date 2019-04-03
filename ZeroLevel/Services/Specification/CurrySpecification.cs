using System;

namespace ZeroLevel.Specification
{
    public class CurrySpecification<T, R> : BaseSpecification<T>
    {
        private readonly Func<T, R> _selector;
        private readonly R _value;

        public CurrySpecification(Func<T, R> selector, R val)
        {
            _selector = selector;
            _value = val;
        }

        public override bool IsSatisfiedBy(T o)
        {
            return _selector(o).Equals(_value);
        }
    }
}