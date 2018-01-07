using System;
using System.Collections.Generic;

namespace RESTar.Admin
{
    [RESTar(Methods.GET)]
    internal class Console : ISelector<Console>, ICounter<Console>
    {
        public IEnumerable<Console> Select(IRequest<Console> request) => throw new NotImplementedException();
        public long Count(IRequest<Console> request) => 0;
    }
}