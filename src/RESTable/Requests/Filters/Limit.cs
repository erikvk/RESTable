using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace RESTable.Requests.Filters
{
    /// <inheritdoc cref="IFilter" />
    /// <summary>
    /// Encodes a numeric limit used in requests. Can be implicitly converted from int.
    /// </summary>
    public readonly struct Limit : IFilter
    {
        public readonly int Number;

        public static Limit NoLimit => -1;

        private Limit(int nr) => Number = nr;

        public static implicit operator Limit(int nr) => new(nr);
        public static explicit operator int(Limit limit) => limit.Number;

        public static bool operator ==(Limit l, int i) => l.Number == i;
        public static bool operator !=(Limit l, int i) => l.Number != i;
        public static bool operator ==(int i, Limit l) => l.Number == i;
        public static bool operator !=(int i, Limit l) => l.Number != i;
        public static bool operator <(Limit l, int i) => l.Number < i;
        public static bool operator >(Limit l, int i) => l.Number > i;
        public static bool operator <=(Limit l, int i) => l.Number <= i;
        public static bool operator >=(Limit l, int i) => l.Number >= i;


        public bool Equals(Limit other) => Number == other.Number;
        public override bool Equals(object obj) => obj is Limit limit && Equals(limit);
        public override int GetHashCode() => Number.GetHashCode();
        public override string ToString() => Number.ToString();

        /// <summary>
        /// Applies the limiting to an IEnumerable of entities
        /// </summary>
        public IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : class
        {
            if (Number > -1)
                return entities.Take(Number);
            return entities;
        }
    }
}