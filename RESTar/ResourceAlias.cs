using Starcounter;
using System;
using System.Collections.Generic;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class ResourceAlias
    {
        public string Alias;
        private string _resource;

        public string Resource
        {
            get { return _resource; }
            set
            {
                try
                {
                    var r = RESTarConfig.ResourcesDict[value.ToLower()];
                    _resource = r.FullName;
                }
                catch (KeyNotFoundException)
                {
                    this.Delete();
                    var match = value.FindResource();
                    throw new UnknownResourceForMappingException(value, match);
                }
                catch
                {
                    this.Delete();
                    throw;
                }
            }
        }

        public Type GetResource() => RESTarConfig.ResourcesDict[Resource.ToLower()];

        public static Type FindByAlias(string alias)
        {
            return DB.Get<ResourceAlias>("Alias", alias)?.GetResource();
        }

        public static bool Exists(Type resource) => DB.Exists<ResourceAlias>("Resource", resource.FullName);

        public static bool NotExists(Type resource) => !Exists(resource);
    }
}