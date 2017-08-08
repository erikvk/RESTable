using Starcounter;

#pragma warning disable 1591

namespace RESTar
{
    [Database]
    public class ResourceAlias
    {
        public string Alias;
        private string _resource;

        public string Resource
        {
            get => _resource;
            set => _resource = value;
        }
    }
}