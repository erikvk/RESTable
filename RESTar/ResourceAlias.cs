using System;
using System.Collections.Generic;
using RESTar.Internal;
using Starcounter;
using static RESTar.RESTarPresets;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    [Database, RESTar(ReadAndWrite, Viewable = true)]
    public class ResourceAlias
    {
        [UniqueId]
        public string Alias;
        private string _resource;

        public string Resource
        {
            get { return _resource; }
            set
            {
                try
                {
                    if (value.StartsWith("RESTar.DynamicResource"))
                    {
                        _resource = DynamitControl.GetByTableNameLower(value.ToLower()).FullName;
                        return;
                    }
                    var r = RESTarConfig.NameResources[value.ToLower()];
                    _resource = r.Name;
                }
                catch (KeyNotFoundException)
                {
                    this.Delete();
                    var match = value.FindResource();
                    throw new UnknownResourceForAliasException(value, match.TargetType);
                }
                catch
                {
                    this.Delete();
                    throw;
                }
            }
        }

        public IResource GetResource() => RESTarConfig.NameResources[Resource.ToLower()];

        public static IResource ByAlias(string alias)
        {
            return DB.Get<ResourceAlias>("Alias", alias)?.GetResource();
        }

        public static string ByResource(Type resource)
        {
            return DB.Get<ResourceAlias>("Resource", resource.FullName)?.Alias;
        }

        public static bool Exists(Type resource) => DB.Exists<ResourceAlias>("Resource", resource.FullName);

        public static bool NotExists(Type resource) => !Exists(resource);
    }
}