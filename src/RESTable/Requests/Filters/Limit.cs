using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace RESTable.Requests.Filters
{
    /// <inheritdoc cref="IFilter" />
    /// <summary>
    /// Encodes a numeric limit used in requests. Can be implicitly converted from int.
    /// </summary>
    public struct Limit : IFilter
    {
        public readonly long Number;
        public bool Equals(Limit other) => Number == other.Number;
        public override bool Equals(object obj) => obj is Limit limit && Equals(limit);
        public override int GetHashCode() => Number.GetHashCode();
        public override string ToString() => Number.ToString();
        public static Limit NoLimit => (Limit) (-1);
        public static explicit operator Limit(long nr) => new Limit(nr);
        public static explicit operator long(Limit limit) => limit.Number;
        public static bool operator ==(Limit l, long i) => l.Number == i;
        public static bool operator !=(Limit l, long i) => l.Number != i;
        public static bool operator ==(long i, Limit l) => l.Number == i;
        public static bool operator !=(long i, Limit l) => l.Number != i;
        public static bool operator <(Limit l, long i) => l.Number < i;
        public static bool operator >(Limit l, long i) => l.Number > i;
        public static bool operator <=(Limit l, long i) => l.Number <= i;
        public static bool operator >=(Limit l, long i) => l.Number >= i;

        private Limit(long nr) => Number = nr;

        /// <summary>
        /// Applies the limiting to an IEnumerable of entities
        /// </summary>
        public IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : class => Number > -1 ? entities.Take((int) Number) : entities;
    }
}