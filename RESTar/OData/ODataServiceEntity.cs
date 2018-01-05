namespace RESTar.OData
{
    internal class ODataServiceEntity
    {
        public string name { get; }
        public string kind { get; }
        public string url { get; }

        private ODataServiceEntity(AvailableResource t) => (name, kind, url) = (t.Name, "EntitySet", t.Name);
        public static ODataServiceEntity Convert(AvailableResource entity) => new ODataServiceEntity(entity);
    }
}