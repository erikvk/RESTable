using RESTar;
using Starcounter;

#pragma warning disable 1591

namespace RESTarExample.TestDb
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public abstract class TestBase
    {
        public ulong star_ObjectNo => this.GetObjectNo();
    }
}