using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// By implementing the <see cref="IValidator{T}"/> interface, entity resources can add custom validation
    /// validation logic that will be called by RESTable each time an entity is inserted or updated in the resource.
    /// </summary>
    public interface IValidator<T> where T : class
    {
        /// <summary>
        /// Validates the entity given as input. If invalid, include a reason in the out parameter to inform the
        /// user of the validation error. Return true if and only if the entity is valid.
        /// </summary>
        IEnumerable<InvalidMember> Validate(T entity, RESTableContext context);
    }

    public static class ValidatorExtensions
    {
        public static InvalidMember Invalidate<TValidator, TMember>
        (
            this TValidator validator,
            Expression<Func<TValidator, TMember>> memberSelector,
            string message
        )
        where TValidator : class, IValidator<TValidator>
        {
            if (memberSelector.Body is not MemberExpression memberExpression)
                throw new ArgumentException("Expected a member expression", nameof(memberSelector));
            return new InvalidMember
            (
                entityType: typeof(TValidator),
                memberName: memberExpression.Member.Name,
                memberType: typeof(TMember),
                message: message
            );
        }
    }
}