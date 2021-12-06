using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace RESTable.Requests.Filters;

/// <inheritdoc cref="IFilter" />
/// <summary>
///     Encodes a numeric limit used in requests. Can be implicitly converted from int.
/// </summary>
public readonly struct Limit : IFilter
{
    public readonly int Number;

    public static Limit NoLimit => -1;

    private Limit(int nr)
    {
        Number = nr;
    }

    public static implicit operator Limit(int nr)
    {
        return new(nr);
    }

    public static explicit operator int(Limit limit)
    {
        return limit.Number;
    }

    public static bool operator ==(Limit l, int i)
    {
        return l.Number == i;
    }

    public static bool operator !=(Limit l, int i)
    {
        return l.Number != i;
    }

    public static bool operator ==(int i, Limit l)
    {
        return l.Number == i;
    }

    public static bool operator !=(int i, Limit l)
    {
        return l.Number != i;
    }

    public static bool operator <(Limit l, int i)
    {
        return l.Number < i;
    }

    public static bool operator >(Limit l, int i)
    {
        return l.Number > i;
    }

    public static bool operator <=(Limit l, int i)
    {
        return l.Number <= i;
    }

    public static bool operator >=(Limit l, int i)
    {
        return l.Number >= i;
    }

    public bool Equals(Limit other)
    {
        return Number == other.Number;
    }

    public override bool Equals(object? obj)
    {
        return obj is Limit limit && Equals(limit);
    }

    public override int GetHashCode()
    {
        return Number.GetHashCode();
    }

    public override string ToString()
    {
        return Number.ToString();
    }

    /// <summary>
    ///     Applies the limiting to an IEnumerable of entities
    /// </summary>
    public IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
    {
        if (Number > -1)
            return entities.Take(Number);
        return entities;
    }
}