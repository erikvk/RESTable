using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace RESTar.Operations
{
    /// <summary>
    /// Encodes a numeric limit used in requests. Can be implicitly converted from int.
    /// </summary>
    public struct Limit : IFilter
    {
        public readonly int Number;
        public bool Equals(Limit other) => Number == other.Number;
        public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj is Limit && Equals((Limit) obj);
        public override int GetHashCode() => Number;
        public static Limit NoLimit => (Limit) (-1);
        public static explicit operator Limit(int nr) => new Limit(nr);
        public static explicit operator int(Limit limit) => limit.Number;
        public static bool operator ==(Limit l, int i) => l.Number == i;
        public static bool operator !=(Limit l, int i) => l.Number != i;
        public static bool operator <(Limit l, int i) => l.Number < i;
        public static bool operator >(Limit l, int i) => l.Number > i;
        public static bool operator <=(Limit l, int i) => l.Number <= i;
        public static bool operator >=(Limit l, int i) => l.Number >= i;
        private Limit(int nr) => Number = nr;

        /// <summary>
        /// Applies the limiting to an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) => Number > 0 ? entities.Take(Number) : entities;
    }

    /// <summary>
    /// Encodes a numeric offset used in requests. Can be implicitly converted from int.    
    /// </summary>
    public struct Offset : IFilter
    {
        public readonly int Number;
        public bool Equals(Offset other) => Number == other.Number;
        public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj is Offset && Equals((Offset) obj);
        public override int GetHashCode() => Number;
        public static Offset NoOffset => (Offset) (-1);
        public static explicit operator Offset(int nr) => new Offset(nr);
        public static explicit operator int(Offset limit) => limit.Number;
        public static bool operator ==(Offset l, int i) => l.Number == i;
        public static bool operator !=(Offset l, int i) => l.Number != i;
        public static bool operator <(Offset l, int i) => l.Number < i;
        public static bool operator >(Offset l, int i) => l.Number > i;
        public static bool operator <=(Offset l, int i) => l.Number <= i;
        public static bool operator >=(Offset l, int i) => l.Number >= i;
        private Offset(int nr) => Number = nr;

        /// <summary>
        /// Applies the offset to an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) => Number > 0 ? entities.Skip(Number) : entities;
    }
}