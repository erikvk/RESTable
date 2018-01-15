using System;
using System.Collections.Generic;

namespace RESTar.Admin
{
    [RESTar(Methods.GET)]
    internal class ActiveTerminal : ISelector<ActiveTerminal>
    {
        public string Id { get; private set; }
        public string TerminalName { get; private set; }
        public DateTime Opened { get; private set; }
        public string ClientIP { get; private set; }
        public long BytesReceived { get; private set; }
        public long BytesSent { get; private set; }

        public IEnumerable<ActiveTerminal> Select(IRequest<ActiveTerminal> request)
        {
            return null;
        }
    }
}