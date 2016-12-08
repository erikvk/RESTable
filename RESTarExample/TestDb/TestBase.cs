using RESTar;
using Starcounter;

namespace RESTarExample.TestDb
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public abstract class TestBase
    {
        public ulong star_ObjectNo => this.GetObjectNo();
    }
}
