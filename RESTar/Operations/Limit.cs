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
        public static Limit NoLimit => -1;
        public static implicit operator Limit(int nr) => new Limit(nr);
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
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) => Number < 1
            ? entities
            : entities.Take(Number);
    }
}