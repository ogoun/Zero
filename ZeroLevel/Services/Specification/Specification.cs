using System;
using System.Linq.Expressions;

namespace ZeroLevel.Specification
{
    public class Specification<T>
    {
        private Expression<Func<T, bool>> _condition;
        public Expression<Func<T, bool>> Condition { get { return _condition; } }

        public Specification(Expression<Func<T, bool>> condition)
        {
            _condition = condition;
        }

        public bool IsSatisfiedBy(T o)
        {
            return _condition.Compile()(o);
        }

        public Specification<T> And(Func<T, bool> condition)
        {
            _condition = _condition.AndFunc(condition);
            return this;
        }

        public Specification<T> Or(Func<T, bool> condition)
        {
            _condition = _condition.OrFunc(condition);
            return this;
        }

        public Specification<T> Not()
        {
            _condition = _condition.Not();
            return this;
        }
    }
}