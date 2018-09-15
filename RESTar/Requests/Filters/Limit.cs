using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;

#pragma warning disable 1591

namespace RESTar.Requests.Filters
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
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) => Number > -1 ? entities.Take((int) Number) : entities;
    }

    /// <inheritdoc cref="IFilter" />
    /// <summary>
    /// Encodes a numeric offset used in requests. Can be implicitly converted from int.    
    /// </summary>
    public struct Offset : IFilter
    {
        public readonly long Number;
        public bool Equals(Offset other) => Number == other.Number;
        public override bool Equals(object obj) => obj is Offset offset && Equals(offset);
        public override int GetHashCode() => Number.GetHashCode();
        public override string ToString() => Number.ToString();
        public static Offset NoOffset => (Offset) 0;
        public static explicit operator Offset(int nr) => new Offset(nr);
        public static explicit operator long(Offset limit) => limit.Number;
        public static bool operator ==(Offset o, long i) => o.Number == i;
        public static bool operator !=(Offset o, long i) => o.Number != i;
        public static bool operator <(Offset o, long i) => o.Number < i;
        public static bool operator >(Offset o, long i) => o.Number > i;
        public static bool operator <=(Offset o, long i) => o.Number <= i;
        public static bool operator >=(Offset o, long i) => o.Number >= i;
        public static Offset operator +(Offset o, long i) => new Offset(o.Number + i);

        private Offset(long nr) => Number = nr;

        /// <summary>
        /// Applies the offset to an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            switch ((int) Number)
            {
                case 0: return entities;
                case var positive when positive > 0: return entities.Skip(positive);
                case var negative: return entities.TakeLast(-negative);
            }
        }
    }
}