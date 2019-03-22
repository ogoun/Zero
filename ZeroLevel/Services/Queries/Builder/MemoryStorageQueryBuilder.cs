using System;
using System.Linq;
using System.Linq.Expressions;
using ZeroLevel.Services.ObjectMapping;
using ZeroLevel.Services.Reflection;
using ZeroLevel.Specification;

namespace ZeroLevel.Patterns.Queries
{
    internal sealed class MemoryQuery<T>
        : IRealQuery<T, Func<T, bool>>
    {
        public MemoryQuery(Func<T, bool> predicate)
        {
            this.Query = predicate;
        }

        public Func<T, bool> Query { get; }
    }

    internal sealed class MemoryStorageQueryBuilder<T> :
        IQueryBuilder<T, Func<T, bool>>
    {
        public IRealQuery<T, Func<T, bool>> Build(IQuery query)
        {
            var exp = ResolveQuery(query);
            return new MemoryQuery<T>(exp.Compile());
        }

        private static Expression<Func<T, bool>> ResolveQuery(IQuery query)
        {
            if (query is AndQuery)
            {
                return ResolveQueryAnd(query as AndQuery);
            }
            else if (query is OrQuery)
            {
                return ResolveQueryOr(query as OrQuery);
            }
            else if (query is NotQuery)
            {
                return ResolveQueryNot(query as NotQuery);
            }
            return ResolveQueryOp(query as QueryOp);
        }

        private static Expression<Func<T, bool>> ResolveQueryOp(QueryOp query)
        {
            if (query.Operation == QueryOperation.ALL)
            {
                return Expression.Lambda<Func<T, bool>>(Expression.Constant(true, typeof(bool)), new[] { Expression.Parameter(typeof(object)) });
            }

            var mapper = TypeMapper.Create<T>(true);
            var argument = Expression.Parameter(typeof(T));
            Expression param;
            if (mapper[query.FieldName].IsField)
            {
                param = Expression.Field(argument, query.FieldName);
            }
            else
            {
                param = Expression.Property(argument, query.FieldName);
            }
            object value;
            Expression constant;
            if (query.Operation == QueryOperation.IN)
            {
                if (TypeHelpers.IsArray(mapper[query.FieldName].ClrType.GetElementType()) ||
                    TypeHelpers.IsEnumerable(mapper[query.FieldName].ClrType.GetElementType()))
                {
                    value = Convert.ChangeType(query.Value, mapper[query.FieldName].ClrType.GetElementType());
                    constant = Expression.Constant(value, mapper[query.FieldName].ClrType.GetElementType());
                }
                else
                {
                    value = query.Value;
                    constant = Expression.Constant(value, mapper[query.FieldName].ClrType);
                }
            }
            else
            {
                value = Convert.ChangeType(query.Value, mapper[query.FieldName].ClrType);
                constant = Expression.Constant(value, mapper[query.FieldName].ClrType);
            }
            switch (query.Operation)
            {
                case QueryOperation.EQ:
                    return Expression.Lambda<Func<T, bool>>(Expression.Equal(param, constant),
                        new[] { argument });
                case QueryOperation.GT:
                    return Expression.Lambda<Func<T, bool>>(Expression.GreaterThan(param, constant),
                        new[] { argument });
                case QueryOperation.GTE:
                    return Expression.Lambda<Func<T, bool>>(Expression.GreaterThanOrEqual(param, constant),
                        new[] { argument });
                case QueryOperation.LT:
                    return Expression.Lambda<Func<T, bool>>(Expression.LessThan(param, constant),
                        new[] { argument });
                case QueryOperation.LTE:
                    return Expression.Lambda<Func<T, bool>>(Expression.LessThanOrEqual(param, constant),
                        new[] { argument });
                case QueryOperation.NEQ:
                    return Expression.Lambda<Func<T, bool>>(Expression.NotEqual(param, constant),
                        new[] { argument });
                case QueryOperation.IN:
                    var overload = typeof(Enumerable).GetMethods()
                                      .Single(mi => mi.Name.Equals("Contains", StringComparison.Ordinal) && mi.GetParameters().Count() == 2);
                    var call = Expression.Call(
                        overload.MakeGenericMethod(mapper[query.FieldName].ClrType.GetElementType()),
                        param, constant);
                    return Expression.Lambda<Func<T, bool>>(call, new[] { argument });
            }
            return null;
        }

        private static Expression<Func<T, bool>> ResolveQueryAnd(AndQuery query)
        {
            var left = ResolveQuery(query.Left);
            var right = ResolveQuery(query.Right);
            return PredicateBuilder.And(left, right);
        }

        private static Expression<Func<T, bool>> ResolveQueryOr(OrQuery query)
        {
            var left = ResolveQuery(query.Left);
            var right = ResolveQuery(query.Right);
            return PredicateBuilder.Or(left, right);
        }

        private static Expression<Func<T, bool>> ResolveQueryNot(NotQuery query)
        {
            return PredicateBuilder.Not(ResolveQuery(query.Query));
        }
    }
}
