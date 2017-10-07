using System.Collections.Generic;
using RESTar;

// ReSharper disable All

namespace RESTarExample
{
    [RESTar(Methods.GET)]
    public class SQLiteTester : ISelector<SQLiteTester>
    {
        public string Status { get; set; }

        public IEnumerable<SQLiteTester> Select(IRequest<SQLiteTester> request)
        {

            return new[] {new SQLiteTester {Status = "Success"}};
        }
    }
}