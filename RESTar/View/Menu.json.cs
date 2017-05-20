using System.Linq;
using Starcounter;

namespace RESTar.View
{
    partial class Menu : Json
    {
        public Menu Populate()
        {
            MetaResourcePath = $"{Settings._ViewUri}/{typeof(Resource).FullName}";
            RESTarConfig
                .Resources
                .Where(r => r.Viewable)
                .Select(r => new
                {
                    Name = r.AliasOrName,
                    Url = $"{Settings._ViewUri}/{r.AliasOrName}"
                }.SerializeDyn())
                .ForEach(str => Resources.Add(new Json(str)));
            return this;
        }
    }
}