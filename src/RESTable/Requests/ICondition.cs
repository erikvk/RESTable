using RESTable.Meta;

namespace RESTable.Requests;

/// <summary>
///     A non-generic interface for conditions
/// </summary>
public interface ICondition
{
    /// <summary>
    ///     The key of the condition
    /// </summary>
    string Key { get; }

    /// <summary>
    ///     The operator of the condition
    /// </summary>
    Operators Operator { get; }

    /// <summary>
    ///     The value of the condition
    /// </summary>
    object? Value { get; }

    /// <summary>
    ///     The term describing the property to compare with
    /// </summary>
    Term Term { get; }

    /// <summary>
    ///     A string describing the value encoded in the condition
    /// </summary>
    string ValueLiteral { get; }
}
