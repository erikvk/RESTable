using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RESTar;

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