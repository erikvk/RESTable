using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests;

/// <inheritdoc cref="ICondition" />
/// <inheritdoc cref="IUriCondition" />
/// <summary>
///     A condition encodes a predicate that is either true or false of an entity
///     in a resource. It is used to match entities in resources while selecting
///     entities to include in a GET, PUT, PATCH or DELETE request.
/// </summary>
public class Condition<T> : ICondition, IUriCondition where T : class
{
    private object? _value;

    public Condition(Term term, Operators op, object? value)
    {
        // The value literal is set from the Value setter
        ValueLiteral = null!;

        Term = term;
        Operator = op;
        Value = value;
        Skip = term.ConditionSkip;
        Predicate = op switch
        {
            Operators.EQUALS => EqualsPredicate,
            Operators.NOT_EQUALS => NotEqualsPredicate,
            Operators.LESS_THAN => LessThanPredicate,
            Operators.GREATER_THAN => GreaterThanPredicate,
            Operators.LESS_THAN_OR_EQUALS => LessThanOrEqualsPredicate,
            Operators.GREATER_THAN_OR_EQUALS => GreaterThanOrEqualsPredicate,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private Predicate<object?> Predicate { get; }

    /// <summary>
    ///     Should this condition be skipped during evaluation?
    /// </summary>
    public bool Skip { get; set; }

    public Operator ParsedOperator => Operator;

    public Type? Type => Term.IsDeclared ? Term.LastAs<DeclaredProperty>()?.Type : null;

    /// <inheritdoc cref="ICondition" />
    /// <inheritdoc cref="IUriCondition" />
    public string Key => Term.Key;

    /// <summary>
    ///     The operator of the condition, specifies the operation of the truth
    ///     evaluation. Should the condition check for equality, for example?
    /// </summary>
    public Operators Operator { get; }

    /// <summary>
    ///     The second operand for the operation defined by the operator. Defines
    ///     the object for comparison.
    /// </summary>
    public object? Value
    {
        get => _value;
        internal set
        {
            _value = value;
            ValueLiteral = _value switch
            {
                null => "null",
                DateTime dt => dt.ToString("O"),
                string str => str,
                decimal dec => dec.ToString(CultureInfo.InvariantCulture),
                double dou => dou.ToString(CultureInfo.InvariantCulture),
                float flo => flo.ToString(CultureInfo.InvariantCulture),
                _ => $"{_value}"
            };
            ValueTypeCode = Type.GetTypeCode(_value?.GetType());
        }
    }

    /// <inheritdoc />
    public Term Term { get; }

    /// <inheritdoc cref="IUriCondition.ValueLiteral" />
    public string ValueLiteral { get; private set; }

    /// <inheritdoc />
    public TypeCode ValueTypeCode { get; private set; }

    public bool IsOfType<T1>()
    {
        return Type == typeof(T1);
    }

    private int Compare(object? other, object? value)
    {
        try
        {
            return Comparer.DefaultInvariant.Compare(other, value);
        }
        catch (ArgumentException) when (Term.IsDynamic && value is not null)
        {
            var convertedOther = Convert.ChangeType(other, value.GetType());
            return Comparer.DefaultInvariant.Compare(convertedOther, value);
        }
    }

    private bool EqualsPredicate(object? other)
    {
        try
        {
            var result = Compare(other, Value);
            return result == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool NotEqualsPredicate(object? other)
    {
        try
        {
            var result = Compare(other, Value);
            return result != 0;
        }
        catch
        {
            return true;
        }
    }

    private bool LessThanPredicate(object? other)
    {
        try
        {
            var result = Compare(other, Value);
            return result < 0;
        }
        catch
        {
            return false;
        }
    }


    private bool GreaterThanPredicate(object? other)
    {
        try
        {
            var result = Compare(other, Value);
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    private bool LessThanOrEqualsPredicate(object? other)
    {
        try
        {
            var result = Compare(other, Value);
            return result <= 0;
        }
        catch
        {
            return false;
        }
    }

    private bool GreaterThanOrEqualsPredicate(object? other)
    {
        try
        {
            var result = Compare(other, Value);
            return result >= 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Returns true if and only if the condition holds for the given subject
    /// </summary>
    public async ValueTask<bool> HoldsFor(T subject)
    {
        if (Skip) return true;
        var termValue = await Term.GetValue(subject).ConfigureAwait(false);
        var subjectValue = termValue.Value;
        return Predicate(subjectValue);
    }

    /// <summary>
    ///     Tries to parse the uri conditions to a list of conditions of the given type.
    /// </summary>
    /// <param name="uriConditions">The uri conditions to parse</param>
    /// <param name="target">The target to which the conditions refer</param>
    /// <param name="conditions">The parsed conditions (if successful)</param>
    /// <param name="error">The error encountered (if unsuccessful)</param>
    /// <param name="termFactory">The termfactory to use when creating condition terms</param>
    /// <param name="conditionCache">The condition cache to read existing conditions from</param>
    /// <returns>True if and only if the uri conditions were sucessfully parsed</returns>
    public static bool TryParse
    (
        IReadOnlyCollection<IUriCondition> uriConditions,
        ITarget<T> target,
        out List<Condition<T>>? conditions,
        out Error? error,
        TermFactory termFactory,
        ConditionCache<T> conditionCache
    )
    {
        try
        {
            conditions = Parse(uriConditions, target, termFactory, conditionCache);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            conditions = null;
            error = e.AsError();
            return false;
        }
    }

    /// <summary>
    ///     Parses and checks the semantics of Conditions object from a conditions of a REST request URI
    /// </summary>
    public static List<Condition<T>> Parse(IReadOnlyCollection<IUriCondition> uriConditions, ITarget<T> target, TermFactory termFactory, ConditionCache<T> cache)
    {
        var list = new List<Condition<T>>(uriConditions.Count);
        foreach (var uriCondition in uriConditions)
        {
            if (cache.TryGetValue(uriCondition, out var condition))
            {
                list.Add(condition);
                continue;
            }
            var term = termFactory.MakeConditionTerm(target, uriCondition.Key);
            var last = term.Last;
            if (last!.AllowedConditionOperators.HasFlag(uriCondition.Operator) == false)
                throw new BadConditionOperator(uriCondition.Key, target, uriCondition.Operator, term, last.AllowedConditionOperators.ToOperators());
            condition = cache[uriCondition] = new Condition<T>
            (
                term,
                uriCondition.Operator,
                uriCondition.ValueLiteral.ParseConditionValue(last as DeclaredProperty)
            );
            list.Add(condition);
        }
        return list;
    }

    public static Condition<T> Create(string key, Operators op, object value)
    {
        var termFactory = ApplicationServicesAccessor.ApplicationServiceProvider.GetRequiredService<TermFactory>();
        var resourceCollection = ApplicationServicesAccessor.ApplicationServiceProvider.GetRequiredService<ResourceCollection>();
        var resource = resourceCollection.SafeGetResource<T>() ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());
        return new Condition<T>
        (
            termFactory.MakeConditionTerm(resource, key),
            op,
            value
        );
    }

    public void Deconstruct(out string key, out object? value)
    {
        key = Key;
        value = Value;
    }
}