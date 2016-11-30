using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadAndWrite)]
    public class TestClass
    {
        public ulong Id;
        public string Name;
        public int Number;
        public decimal Rate;
        public InnerClass InnerClass;

        public ulong star_ObjectNo => this.GetObjectNo();
        public string star_ObjectID => this.GetObjectID();
    }

    [Database]
    [RESTar(RESTarPresets.ReadAndWrite)]
    public class InnerClass
    {
        public int Number;
        public decimal Rate;
    }
}
