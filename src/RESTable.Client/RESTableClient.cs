using System;
using RESTable.Requests;

namespace RESTable.Client
{
    public class RESTableClient : RESTableContext
    {
        public RESTableClient(Requests.Client client, IServiceProvider services) : base(client, services) { }
    }
}