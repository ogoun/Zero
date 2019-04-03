using System;

namespace ZeroLevel
{
    public static class Monades
    {
        #region With

        public static TResult With<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator)
        {
            if (null != o) return evaluator(o);
            return default(TResult);
        }

        #endregion With

        #region Return

        public static TResult Return<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator, TResult failureValue)
        {
            if (null != o) return evaluator(o);
            return failureValue;
        }

        public static TResult Return<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator)
        {
            if (null != o) return evaluator(o);
            return default(TResult);
        }

        #endregion Return

        #region IsNotNull

        public static bool IsNotNull<TInput>(this TInput o)
        {
            if (null != o) return true;
            return false;
        }

        #endregion IsNotNull

        #region If

        public static TInput If<TInput>(this TInput o, Predicate<TInput> evaluator)
        {
            if (null != o) return evaluator(o) ? o : default(TInput);
            return default(TInput);
        }

        public static TOutput Either<TInput, TOutput>(this TInput o, Func<TInput, bool> condition,
            Func<TInput, TOutput> ifTrue, Func<TInput, TOutput> ifFalse)
            => condition(o) ? ifTrue(o) : ifFalse(o);

        public static TOutput Either<TInput, TOutput>(this TInput o, Func<TInput, TOutput> ifTrue,
            Func<TInput, TOutput> ifFalse)
            => o.Either(x => x != null, ifTrue, ifFalse);

        #endregion If

        #region Do

        public static TInput Do<TInput>(this TInput o, Action<TInput> action)
        {
            if (null != o) action(o);
            return o;
        }

        public static TInput Do<TInput>(this TInput o, Action<TInput> action, Action nullHandler)
        {
            if (null != o)
            {
                action(o);
            }
            else
            {
                nullHandler();
            }
            return o;
        }

        #endregion Do
    }
}