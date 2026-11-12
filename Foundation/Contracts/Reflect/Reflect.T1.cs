namespace SecondMonitor.Contracts.Reflect
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class Reflect<T>
    {
        public static PropertyInfo GetProperty<TResult>(Expression<Func<T, TResult>> property)
        {
            if (GetMemberInfo(property) is PropertyInfo info)
            {
                return info;
            }

            throw new ArgumentException("Not a property");
        }

        private static MemberInfo GetMemberInfo(LambdaExpression lambda)
        {
            if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                return ((MemberExpression)lambda.Body).Member;
            }

            throw new ArgumentException("Not a member access", nameof(lambda));
        }
    }
}