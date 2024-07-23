using System.Linq.Expressions;
using System.Reflection;

namespace OmerkckEF.Biscom.ToolKit
{
    public class BisExpression : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression b)
        {
            return base.VisitBinary(b);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            return base.VisitConstant(c);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            return base.VisitUnary(u);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            var expression = Visit(m.Expression);

            if (expression is ConstantExpression consExp)
            {
                object? container = consExp.Value;

                var member = m.Member;
                if (member is FieldInfo fInfo)
                {
                    object? value = fInfo.GetValue(container);
                    return Expression.Constant(value);
                }

                if (member is PropertyInfo pInfo)
                {
                    object? value = pInfo.GetValue(container, null);
                    return Expression.Constant(value);
                }
            }

            return base.VisitMember(m);
        }

        public Expression ModifyExpression(Expression exp)
        {
            return Visit(exp);
        }

        public static string ConvertExpressionToString(Expression expression)
        {
            if (typeof(BinaryExpression).IsAssignableFrom(expression.GetType()))
            {
                var binary = (BinaryExpression)expression;
                string nodeType = binary.NodeType switch
                {
                    ExpressionType.And => "And",
                    ExpressionType.Or => "Or",
                    ExpressionType.AndAlso => "And",
                    ExpressionType.OrElse => "Or",
                    ExpressionType.Equal => "=",
                    ExpressionType.NotEqual => "!=",
                    ExpressionType.LessThan => "<",
                    ExpressionType.LessThanOrEqual => "<=",
                    ExpressionType.GreaterThan => ">",
                    ExpressionType.GreaterThanOrEqual => ">=",
                    _ => $"--Binary expression '{binary.NodeType}' not supported.--",
                };
                string rightExpression = ConvertExpressionToString(binary.Right);
                if (rightExpression == null)
                {
                    if (nodeType == "=")
                    {
                        nodeType = "is null";
                    }
                    else if (nodeType == "!=")
                    {
                        nodeType = "is not null";
                    }
                }
                return $"({ConvertExpressionToString(binary.Left)} {nodeType} {rightExpression})";
            }
            else if (typeof(MemberExpression).IsAssignableFrom(expression.GetType()))
            {
                var member = (MemberExpression)expression;
                if (member.NodeType == ExpressionType.MemberAccess)
                {
                    if (member.Member.Name == "Length")
                        return $"{ConvertExpressionToString(member.Expression!)}.length";
                    else
                        return $"{member.Expression?.Type.Name}.{member.Member.Name}";
                }
            }
            else if (typeof(ParameterExpression).IsAssignableFrom(expression.GetType()))
            {
                var parameter = (ParameterExpression)expression;
                return $"{parameter.Name}";
            }
            else if (typeof(ConstantExpression).IsAssignableFrom(expression.GetType()))
            {
                var constant = (ConstantExpression)expression;
                if (constant.Value == null)
                    return string.Empty;
                else
                {
                    if (typeof(int).IsAssignableFrom(constant.Value.GetType()))
                        return constant.Value.ToString() ?? "";
                    else if (typeof(string).IsAssignableFrom(constant.Value.GetType()))
                        return $"'{constant.Value}'";
                    else if (typeof(bool).IsAssignableFrom(constant.Value.GetType()))
                        return constant.Value.ToString() ?? "";
                    else if (typeof(List<int>).IsAssignableFrom(constant.Value.GetType()))
                        return $"({string.Join(",", (List<int>)constant.Value)})";
                    else if (typeof(int[]).IsAssignableFrom(constant.Value.GetType()))
                        return $"({string.Join(",", (int[])constant.Value)})";
                    else if (typeof(string[]).IsAssignableFrom(constant.Value.GetType()))
                        return $"('{string.Join("','", (string[])constant.Value)}')";
                    else if (typeof(List<string>).IsAssignableFrom(constant.Value.GetType()))
                        return $"('{string.Join("','", (List<string>)constant.Value)}')";
                    else if (typeof(List<object>).IsAssignableFrom(constant.Value.GetType()))
                        return $"('{string.Join("','", (List<object>)constant.Value)}')";

                }
            }
            else if (typeof(LambdaExpression).IsAssignableFrom(expression.GetType()))
            {
                var lambda = (LambdaExpression)expression;
                return $"{ConvertExpressionToString(lambda.Body)}";
            }
            else if (typeof(MethodCallExpression).IsAssignableFrom(expression.GetType()))
            {
                var method = (MethodCallExpression)expression;

                switch (method.Method.Name)
                {
                    case "IsNullOrEmpty":
                        return $"(!{ConvertExpressionToString(method.Arguments[0])})";
                    case "Contains":
                        if (method.Object == null)
                        {
                            var listValues = method.Arguments[0] as ConstantExpression;
                            if (listValues != null)
                            {
                                if (listValues.Value is IEnumerable<int> intList)
                                {
                                    var ids = string.Join(",", intList);
                                    return $"({ConvertExpressionToString(method.Arguments[1])} IN ({ids}))";
                                }
                                else if (listValues.Value is IEnumerable<string> stringList)
                                {
                                    var ids = string.Join(",", stringList.Select(id => $"'{id}'"));
                                    return $"({ConvertExpressionToString(method.Arguments[1])} IN ({ids}))";
                                }
                            }
                        }
                        else
                        {
                            // method.Object null değilse, string veya başka bir nesne üzerinde Contains metodu çalıştırılıyor demektir
                            var obj = ConvertExpressionToString(method.Object);
                            var arg = ConvertExpressionToString(method.Arguments[0]);

                            // Eğer method.Object ve method.Arguments[0] bir string ise LIKE ifadesi kullanılır
                            if (method.Object.Type == typeof(string) && method.Arguments[0].Type == typeof(string))
                            {
                                return $"({obj} LIKE ('%{arg.Replace("'", "")}%'))";
                            }
                            else if (method.Object is ConstantExpression constExpr &&
                                     (constExpr.Value is IEnumerable<int> || constExpr.Value is IEnumerable<string>))
                            {
                                // method.Object bir listeyi temsil eden ConstantExpression ise, IN ifadesi kullanılmalıdır
                                if (constExpr.Value is IEnumerable<int> intList)
                                {
                                    var ids = string.Join(",", intList);
                                    return $"({ConvertExpressionToString(method.Arguments[0])} IN ({ids}))";
                                }
                                else if (constExpr.Value is IEnumerable<string> stringList)
                                {
                                    var ids = string.Join(",", stringList.Select(id => $"'{id}'"));
                                    return $"({ConvertExpressionToString(method.Arguments[0])} IN ({ids}))";
                                }
                            }
                            else
                            {
                                // Başka tipler için burada uygun dönüşümler yapılabilir
                                return $"--Method '{method.Method.Name}' not supported for non-string objects.--";
                            }
                        }
                        break;
                    case "StartsWith":
                        return $"({ConvertExpressionToString(method.Object!)} LIKE '{ConvertExpressionToString(method.Arguments[0])}%')";
                    case "EndsWith":
                        return $"({ConvertExpressionToString(method.Object!)} LIKE '%{ConvertExpressionToString(method.Arguments[0])}')";
                    default:
                        return $"--Method '{method.Method.Name}' not supported.--";
                }
            }
            else if (typeof(UnaryExpression).IsAssignableFrom(expression.GetType()))
            {
                var unary = (UnaryExpression)expression;
                string nodeType;
                switch (unary.NodeType)
                {
                    case ExpressionType.Not:
                        nodeType = "!";
                        break;
                    case ExpressionType.Negate:
                        nodeType = "-";
                        break;
                    case ExpressionType.Convert:
                        //nodeType = $"Convert({unary.Operand.Type}, {unary.Operand})";
                        return ConvertExpressionToString(unary.Operand);
                    default:
                        nodeType = $"--Unary expression '{unary.NodeType}' not supported.--";
                        break;
                }
                return $"{nodeType}{ConvertExpressionToString(unary.Operand)}";
            }

            return $"--Expression of type '{expression.GetType().Name}' not supported.--";
        }
    }

    public static class ExpressionExtensions
    {
        public static string? ConvertExpressionToQueryString<T>(this Expression<Func<T, bool>> ReceivedExp)
        {
            if (ReceivedExp == null) return default;

            BisExpression expVisitor = new();

            Expression exp = expVisitor.ModifyExpression(ReceivedExp.Body);

            return BisExpression.ConvertExpressionToString(exp);
        }
    }
}
