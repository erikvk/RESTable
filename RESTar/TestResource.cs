using System.Collections.Generic;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class TestResource : IInserter<TestResource>
    {
        public string s;
        public int i;

        public void Insert(IEnumerable<TestResource> entities, IRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
