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
				string nodeType;
				switch (binary.NodeType)
				{
					case ExpressionType.And:
						nodeType = "And";
						break;
					case ExpressionType.Or:
						nodeType = "Or";
						break;
					case ExpressionType.AndAlso:
						nodeType = "And";
						break;
					case ExpressionType.OrElse:
						nodeType = "Or";
						break;
					case ExpressionType.Equal:
						nodeType = "=";
						break;
					case ExpressionType.NotEqual:
						nodeType = "!=";
						break;
					case ExpressionType.LessThan:
						nodeType = "<";
						break;
					case ExpressionType.LessThanOrEqual:
						nodeType = "<=";
						break;
					case ExpressionType.GreaterThan:
						nodeType = ">";
						break;
					case ExpressionType.GreaterThanOrEqual:
						nodeType = ">=";
						break;
					default:
						nodeType = $"--Binary expression '{binary.NodeType}' not supported.--";
						break;
				}
				return $"({ConvertExpressionToString(binary.Left)} {nodeType} {ConvertExpressionToString(binary.Right)})";
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
					return "''";
				else
				{
					if (typeof(int).IsAssignableFrom(constant.Value.GetType()))
						return constant.Value.ToString() ?? "";
					else if (typeof(string).IsAssignableFrom(constant.Value.GetType()))
						return $"'{constant.Value}'";
					else if (typeof(bool).IsAssignableFrom(constant.Value.GetType()))
						return constant.Value.ToString() ?? "";
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
						return $"({ConvertExpressionToString(method.Object!)} LIKE '%{ConvertExpressionToString(method.Arguments[0])}%')";
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
		public static string ConvertExpressionToQueryString<T>(this Expression<Func<T, bool>> ReceivedExp)
		{
			BisExpression expVisitor = new();

			Expression exp = expVisitor.ModifyExpression(ReceivedExp.Body);

			return BisExpression.ConvertExpressionToString(exp);
		}
	}
}
