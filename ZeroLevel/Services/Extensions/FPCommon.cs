using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.Extensions
{
    /// <summary>
    /// Обобщенные возможности из ФП
    /// </summary>
    public static class FPCommon
    {
        /*
         * Func<int,int,int> add = (x,y) => x + y; 
         * Func<int,Func<int,int>> curriedAdd = add.Curry();
         * Func<int,int> inc = curriedAdd(1);
         */
        /// <summary>
        /// Каррирование
        /// </summary>
        public static Func<A, Func<B, R>> Curry<A, B, R>(this Func<A, B, R> f)
        {
            return a => b => f(a, b);
        }
        /*
         * Func<int,int,int> add = (x,y) => x + y; 
         * Func<int,int> inc = add.Partial(1);
         */
        /// <summary>
        /// Частичное исполнение
        /// </summary>
        public static Func<B, R> Partial<A, B, R>(this Func<A, B, R> f, A a)
        {
            return b => f(a, b);
        }
        /// <summary>
        /// PipeTo
        /// </summary>
        /*
         * Before
         * public IActionResult Get() {var someData = query.Where(x => x.IsActive).OrderBy(x => x.Id).ToArray();return Ok(someData);}
         * After
         * public IActionResult Get() =>  query.Where(x => x.IsActive).OrderBy(x => x.Id).ToArray().PipeTo(Ok);
         */
        public static TResult PipeTo<TSource, TResult>(this TSource source, Func<TSource, TResult> func) => func(source);
    }

    public class Either<TL, TR>
    {
        [DataMember]
        private readonly bool _isLeft;

        [DataMember]
        private readonly TL _left;

        [DataMember]
        private readonly TR _right;

        public Either(TL left)
        {
            _left = left;
            _isLeft = true;
        }

        public Either(TR right)
        {
            _right = right;
            _isLeft = false;
        }

        /// <summary>
        /// Checks the type of the value held and invokes the matching handler function.
        /// </summary>
        /// <typeparam name="T">The return type of the handler functions.</typeparam>
        /// <param name="ofLeft">Handler for the Left type.</param>
        /// <param name="ofRight">Handler for the Right type.</param>
        /// <returns>The value returned by the invoked handler function.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public T Match<T>(Func<TL, T> ofLeft, Func<TR, T> ofRight)
        {
            if (ofLeft == null)
            {
                throw new ArgumentNullException(nameof(ofLeft));
            }

            if (ofRight == null)
            {
                throw new ArgumentNullException(nameof(ofRight));
            }

            return _isLeft ? ofLeft(_left) : ofRight(_right);
        }

        /// <summary>
        /// Checks the type of the value held and invokes the matching handler function.
        /// </summary>
        /// <param name="ofLeft">Handler for the Left type.</param>
        /// <param name="ofRight">Handler for the Right type.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public void Match(Action<TL> ofLeft, Action<TR> ofRight)
        {
            if (ofLeft == null)
            {
                throw new ArgumentNullException(nameof(ofLeft));
            }

            if (ofRight == null)
            {
                throw new ArgumentNullException(nameof(ofRight));
            }

            if (_isLeft)
            {
                ofLeft(_left);
            }
            else
            {
                ofRight(_right);
            }
        }

        public TL LeftOrDefault() => Match(l => l, r => default(TL));
        public TR RightOrDefault() => Match(l => default(TR), r => r);
        public Either<TR, TL> Swap() => Match(Right<TR, TL>, Left<TR, TL>);

        public Either<TL, T> Bind<T>(Func<TR, T> f)
            => BindMany(x => Right<TL, T>(f(x)));

        public Either<TL, T> BindMany<T>(Func<TR, Either<TL, T>> f) => Match(Left<TL, T>, f);

        public Either<TL, TResult> BindMany<T, TResult>(Func<TR, Either<TL, T>> f, Func<TR, T, TResult> selector)
            => BindMany(x => f(x).Bind(t => selector(_right, t)));

        public static implicit operator Either<TL, TR>(TL left) => new Either<TL, TR>(left);
        public static implicit operator Either<TL, TR>(TR right) => new Either<TL, TR>(right);

        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left)
            => new Either<TLeft, TRight>(left);

        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right)
            => new Either<TLeft, TRight>(right);

        public static Either<Exception, T> Try<T>(Func<T> f)
        {
            try
            {
                return new Either<Exception, T>(f.Invoke());
            }
            catch (Exception ex)
            {
                return new Either<Exception, T>(ex);
            }
        }
    }
}
