using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Specification
{
    [DataContract]
    public class ExpressionSpecification<T>
        : BaseSpecification<T>
    {
        [DataMember]
        private Func<T, bool> _expression;

        public ExpressionSpecification(Func<T, bool> expression)
        {
            if (expression == null)
                throw new ArgumentNullException();
            else
                this._expression = expression;
        }

        public override bool IsSatisfiedBy(T o)
        {
            return this._expression(o);
        }
    }
}