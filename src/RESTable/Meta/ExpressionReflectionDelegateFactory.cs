using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RESTable.Meta
{
    internal class ExpressionReflectionDelegateFactory
    {
        internal static ExpressionReflectionDelegateFactory Instance { get; } = new();

        public Func<T, object?> CreateGet<T>(PropertyInfo propertyInfo)
        {
            Type instanceType = typeof(T);
            Type resultType = typeof(object);

            ParameterExpression parameterExpression = Expression.Parameter(instanceType, "instance");
            Expression resultExpression;

            MethodInfo? getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod == null)
            {
                throw new ArgumentException("Property does not have a getter.");
            }

            if (getMethod.IsStatic)
            {
                resultExpression = Expression.MakeMemberAccess(null, propertyInfo);
            }
            else
            {
                Expression readParameter = EnsureCastExpression(parameterExpression, propertyInfo.DeclaringType);

                resultExpression = Expression.MakeMemberAccess(readParameter, propertyInfo);
            }

            resultExpression = EnsureCastExpression(resultExpression, resultType);

            LambdaExpression lambdaExpression = Expression.Lambda(typeof(Func<T, object>), resultExpression, parameterExpression);

            Func<T, object?> compiled = (Func<T, object?>) lambdaExpression.Compile();
            return compiled;
        }

        public Action<T, object?> CreateSet<T>(PropertyInfo propertyInfo)
        {
            // use reflection for structs
            // expression doesn't correctly set value
            if (propertyInfo.DeclaringType?.IsValueType == true)
            {
                return (o, v) => propertyInfo.SetValue(o, v, null);
            }

            Type instanceType = typeof(T);
            Type valueType = typeof(object);

            ParameterExpression instanceParameter = Expression.Parameter(instanceType, "instance");

            ParameterExpression valueParameter = Expression.Parameter(valueType, "value");
            Expression readValueParameter = EnsureCastExpression(valueParameter, propertyInfo.PropertyType);

            MethodInfo? setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod == null)
            {
                throw new ArgumentException("Property does not have a setter.");
            }

            Expression setExpression;
            if (setMethod.IsStatic)
            {
                setExpression = Expression.Call(setMethod, readValueParameter);
            }
            else
            {
                Expression readInstanceParameter = EnsureCastExpression(instanceParameter, propertyInfo.DeclaringType);

                setExpression = Expression.Call(readInstanceParameter, setMethod, readValueParameter);
            }

            LambdaExpression lambdaExpression = Expression.Lambda(typeof(Action<T, object?>), setExpression, instanceParameter, valueParameter);

            Action<T, object?> compiled = (Action<T, object?>) lambdaExpression.Compile();
            return compiled;
        }

        private Expression EnsureCastExpression(Expression expression, Type targetType, bool allowWidening = false)
        {
            Type expressionType = expression.Type;

            // check if a cast or conversion is required
            if (expressionType == targetType || !expressionType.IsValueType && targetType.IsAssignableFrom(expressionType))
            {
                return expression;
            }

            if (targetType.IsValueType)
            {
                Expression convert = Expression.Unbox(expression, targetType);

                if (allowWidening && targetType.IsPrimitive)
                {
                    MethodInfo toTargetTypeMethod = typeof(Convert)
                        .GetMethod("To" + targetType.Name, new[] {typeof(object)});

                    if (toTargetTypeMethod != null)
                    {
                        convert = Expression.Condition(
                            Expression.TypeIs(expression, targetType),
                            convert,
                            Expression.Call(toTargetTypeMethod, expression));
                    }
                }

                return Expression.Condition(
                    Expression.Equal(expression, Expression.Constant(null, typeof(object))),
                    Expression.Default(targetType),
                    convert);
            }

            return Expression.Convert(expression, targetType);
        }
    }
}