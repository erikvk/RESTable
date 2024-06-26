﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     By implementing the <see cref="IValidator{T}" /> interface, entity resources can add custom validation
///     validation logic that will be called by RESTable each time an entity is inserted or updated in the resource.
/// </summary>
public interface IValidator<in T> where T : class
{
    /// <summary>
    ///     Validates the entity given as input. If invalid, include a reason in the out parameter to inform the
    ///     user of the validation error. Return true if and only if the entity is valid.
    /// </summary>
    IEnumerable<InvalidMember> GetInvalidMembers(T entity, RESTableContext context);
}

public static class ValidatorExtensions
{
    public static InvalidMember MemberInvalid<TValidator, TMember>
    (
        this TValidator validator,
        Expression<Func<TValidator, TMember>> memberSelector,
        string invalidReason = "had a missing or invalid value"
    )
        where TValidator : class, IValidator<TValidator>
    {
        if (memberSelector.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Expected a member expression", nameof(memberSelector));
        return new InvalidMember
        (
            typeof(TValidator),
            memberExpression.Member.Name,
            typeof(TMember),
            invalidReason
        );
    }

    public static InvalidMember MemberNull<TValidator, TMember>
    (
        this TValidator validator,
        Expression<Func<TValidator, TMember>> memberSelector
    )
        where TValidator : class, IValidator<TValidator>
    {
        return validator.MemberInvalid(memberSelector, "was null");
    }
}
