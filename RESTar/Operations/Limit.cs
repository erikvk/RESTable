using System.Collections.Generic;
using System.Linq;

namespace RESTar.Operations
{
    public struct Limit : IFilter
    {
        public int Number;
        public static Limit NoLimit => -1;
        public static implicit operator Limit(int nr) => new Limit(nr);
        public static bool operator ==(Limit l, int i) => l.Number == i;
        public static bool operator !=(Limit l, int i) => l.Number != i;
        public static bool operator <(Limit l, int i) => l.Number < i;
        public static bool operator >(Limit l, int i) => l.Number > i;
        public static bool operator <=(Limit l, int i) => l.Number <= i;
        public static bool operator >=(Limit l, int i) => l.Number >= i;

        private Limit(int nr)
        {
            Number = nr;
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) => Number < 1
            ? entities
            : entities.Take(Number);
    }
}