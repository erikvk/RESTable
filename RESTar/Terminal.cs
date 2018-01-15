using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar.WebSockets
{
    [RESTar(Methods.GET)]
    internal class Terminal : ISelector<Terminal>
    {
        private static Dictionary<string, Type> Terminals;
        static Terminal() => Terminals = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public string Name { get; private set; }
        public string Description { get; private set; }

        public IEnumerable<Terminal> Select(IRequest<Terminal> request)
        {

        }
    }
}