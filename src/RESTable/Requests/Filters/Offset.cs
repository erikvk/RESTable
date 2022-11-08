using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESTable.Requests.Filters;

/// <inheritdoc cref="IFilter" />
/// <summary>
///     Encodes a numeric offset used in requests. Can be implicitly converted from int.
/// </summary>
public readonly struct Offset : IFilter
{
    public static Offset NoOffset => 0;

    public readonly int Number;

    public static implicit operator Offset(int nr)
    {
        return new(nr);
    }

    public static explicit operator int(Offset limit)
    {
        return limit.Number;
    }

    public static bool operator ==(Offset o, int i)
    {
        return o.Number == i;
    }

    public static bool operator !=(Offset o, int i)
    {
        return o.Number != i;
    }

    public static bool operator <(Offset o, int i)
    {
        return o.Number < i;
    }

    public static bool operator >(Offset o, int i)
    {
        return o.Number > i;
    }

    public static bool operator <=(Offset o, int i)
    {
        return o.Number <= i;
    }

    public static bool operator >=(Offset o, int i)
    {
        return o.Number >= i;
    }

    private Offset(int nr)
    {
        Number = nr;
    }

    public bool Equals(Offset other)
    {
        return Number == other.Number;
    }

    public override bool Equals(object? obj)
    {
        return obj is Offset offset && Equals(offset);
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
    ///     Applies the offset to an IEnumerable of entities
    /// </summary>
    public async IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
    {
        switch (Number)
        {
            case int.MaxValue:
            case int.MinValue:
            {
                yield break;
            }
            case 0:
            {
                await foreach (var item in entities.ConfigureAwait(false))
                    yield return item;
                yield break;
            }
            case > 0:
            {
                await foreach (var item in entities.Skip(Number).ConfigureAwait(false))
                    yield return item;
                yield break;
            }
            case < 0:
            {
                await foreach (var item in NegativeSkip(entities, -Number).ConfigureAwait(false))
                    yield return item;
                yield break;
            }
        }
    }

    /// <summary>
    ///     Returns the last items in the IEnumerable (with just one pass over the IEnumerable)
    /// </summary>
    private static async IAsyncEnumerable<T> NegativeSkip<T>(IAsyncEnumerable<T> source, int count) where T : notnull
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        var queue = new Queue<T>(count);
        await foreach (var element in source.ConfigureAwait(false))
        {
            queue.Enqueue(element);
            if (queue.Count > count)
                queue.Dequeue();
        }
        foreach (var item in queue)
            yield return item;
    }
}
