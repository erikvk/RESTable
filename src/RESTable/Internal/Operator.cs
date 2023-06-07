using System;
using RESTable.Requests;

#pragma warning disable 1591

namespace RESTable.Internal;

/// <summary>
///     Encodes an operator, used in conditions
/// </summary>
public readonly struct Operator
{
    /// <summary>
    ///     The code for this operator
    /// </summary>
    public readonly Operators OpCode;

    /// <summary>
    ///     The common string representation of this operator
    /// </summary>
    internal string Common => OpCode switch
    {
        Operators.EQUALS => "=",
        Operators.NOT_EQUALS => "!=",
        Operators.LESS_THAN => "<",
        Operators.GREATER_THAN => ">",
        Operators.LESS_THAN_OR_EQUALS => "<=",
        Operators.GREATER_THAN_OR_EQUALS => ">=",
        _ => throw new ArgumentOutOfRangeException()
    };

    /// <summary>
    ///     The Sql string representation of this operator
    /// </summary>
    public string Sql => OpCode == Operators.NOT_EQUALS ? "<>" : Common;

    internal bool Equality => OpCode is Operators.EQUALS or Operators.NOT_EQUALS;
    internal bool Compare => !Equality;

    public override bool Equals(object? obj)
    {
        return obj is Operator op && op.OpCode == OpCode;
    }

    public bool Equals(Operator other)
    {
        return OpCode == other.OpCode;
    }

    public override int GetHashCode()
    {
        return (int) OpCode;
    }

    public override string ToString()
    {
        return Common;
    }

    public static bool operator ==(Operator o1, Operator o2)
    {
        return o1.OpCode == o2.OpCode;
    }

    public static bool operator !=(Operator o1, Operator o2)
    {
        return !(o1 == o2);
    }

    public static Operator EQUALS => Operators.EQUALS;
    public static Operator NOT_EQUALS => Operators.NOT_EQUALS;
    public static Operator LESS_THAN => Operators.LESS_THAN;
    public static Operator GREATER_THAN => Operators.GREATER_THAN;
    public static Operator LESS_THAN_OR_EQUALS => Operators.LESS_THAN_OR_EQUALS;
    public static Operator GREATER_THAN_OR_EQUALS => Operators.GREATER_THAN_OR_EQUALS;

    private Operator(Operators op)
    {
        switch (op)
        {
            case Operators.EQUALS:
            case Operators.NOT_EQUALS:
            case Operators.LESS_THAN:
            case Operators.GREATER_THAN:
            case Operators.LESS_THAN_OR_EQUALS:
            case Operators.GREATER_THAN_OR_EQUALS:
                OpCode = op;
                break;
            default: throw new ArgumentException($"Invalid operator '{op}'");
        }
    }

    private static Operator Parse(string common)
    {
        return common switch
        {
            "=" => Operators.EQUALS,
            "!=" => Operators.NOT_EQUALS,
            "<" => Operators.LESS_THAN,
            ">" => Operators.GREATER_THAN,
            "<=" => Operators.LESS_THAN_OR_EQUALS,
            ">=" => Operators.GREATER_THAN_OR_EQUALS,
            _ => throw new ArgumentException(nameof(common))
        };
    }

    internal static bool TryParse(string common, out Operator op)
    {
        try
        {
            op = Parse(common);
            return true;
        }
        catch
        {
            op = default;
            return false;
        }
    }

    public static implicit operator Operator(Operators op)
    {
        return new(op);
    }
}
