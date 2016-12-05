using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;

namespace RESTar.TestDb
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public abstract class TestBase
    {
        public ulong star_ObjectNo => this.GetObjectNo();
    }
}
