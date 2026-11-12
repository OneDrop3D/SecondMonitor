namespace SecondMonitor.Contracts.Reflect
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class Reflect
    {
        public static PropertyInfo GetProperty<TResult>(Expression<Func<TResult>> property)
        {
            if (GetMemberInfo(property) is PropertyInfo info)
            {
                return info;
            }

            throw new ArgumentException("Not a property");
        }

        private static MemberInfo GetMemberInfo(LambdaExpression lambda)
        {
            MemberExpression me = GetMemberExpression(lambda);
            MemberInfo memberInfo = me?.Member;

            if (memberInfo == null)
            {
                throw new ArgumentException("Expression does not refer to a property: " + lambda);
            }

            return memberInfo;
        }

        public static MemberExpression GetMemberExpression(LambdaExpression lambda)
        {
            Expression body = lambda.Body;
            while (body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.ConvertChecked)
            {
                body = ((UnaryExpression)body).Operand;
            }

            MemberExpression me = body as MemberExpression;
            return me;
        }
    }
}