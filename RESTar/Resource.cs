using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using RESTar.Internal;
using RESTar.Operations;
using static System.Reflection.BindingFlags;
using static RESTar.Internal.DynamicResource;

namespace RESTar
{
    [RESTar(RESTarPresets.ReadAndWrite, Visible = true)]
    public sealed class Resource : IOperationsProvider<Resource>
    {
        public string Name { get; set; }
        public bool Editable { get; private set; }
        public bool Visible { get; set; }
        public RESTarMethods[] AvailableMethods { get; set; }
        public string Alias { get; set; }
        public string EntityViewHtmlPath { get; set; }
        public string EntitiesViewHtmlPath { get; set; }
        
        [DataMember(Name = "TargetType")]
        public string TargetTypeString => TargetType?.FullName;

        [IgnoreDataMember]
        public Type TargetType { get; set; }

        public IEnumerable<Resource> Select(IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return RESTarConfig.Resources
                .Filter(request.Conditions)
                .Select(m => new Resource
                {
                    Name = m.Name,
                    Alias = m.Alias,
                    AvailableMethods = m.AvailableMethods,
                    Editable = m.Editable,
                    TargetType = m.TargetType,
                    Visible = m.Visible,
                    EntityViewHtmlPath = m.EntityViewHtml,
                    EntitiesViewHtmlPath = m.EntitiesViewHtml
                });
        }

        public int Insert(IEnumerable<Resource> resources, IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            var dynamicTables = resources.ToList();
            try
            {
                foreach (var entity in dynamicTables)
                {
                    if (string.IsNullOrEmpty(entity.Alias))
                        throw new Exception("No Alias for new resource");
                    if (DB.Exists<ResourceAlias>("Alias", entity.Alias))
                        throw new Exception($"Invalid Alias: '{entity.Alias}' is used to refer to another resource");
                    entity.AvailableMethods = RESTarConfig.Methods;
                    MakeTable(entity);
                    count += 1;
                }
            }
            catch (Exception e)
            {
                throw new AbortedInserterException(e, $"Invalid resource: {e.Message}");
            }
            return count;
        }

        public int Update(IEnumerable<Resource> entities, IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                DeleteTable(resource);
                MakeTable(resource);
                count += 1;
            }
            return count;
        }

        public int Delete(IEnumerable<Resource> entities, IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                DeleteTable(resource);
                count += 1;
            }
            return count;
        }

        private static readonly MethodInfo AUTO_MAKER = typeof(Resource)
            .GetMethod(nameof(AUTO_MAKE), NonPublic | Static);

        internal static void AutoMakeResource(Type type) => AUTO_MAKER
            .MakeGenericMethod(type)
            .Invoke(null, null);

        private static void AUTO_MAKE<T>() where T : class => Resource<T>
            .Make(typeof(T).GetAttribute<RESTarAttribute>());

        public static void Register<T>(RESTarPresets preset, params RESTarMethods[] additionalMethods) where T : class
        {
            var methods = preset.ToMethods().Union(additionalMethods).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null);
        }

        public static void Register<T>(RESTarMethods method, params RESTarMethods[] addMethods) where T : class
        {
            var methods = new[] {method}.Union(addMethods).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null, null);
        }

        public static void Register<T>
        (
            RESTarPresets preset,
            IEnumerable<RESTarMethods> addMethods = null,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null,
            bool visible = false
        ) where T : class
        {
            var methods = preset.ToMethods().Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Register
            (
                method: methods.First(),
                addMethods: methods.Length > 1
                    ? methods.Skip(1).ToArray()
                    : null,
                selector: selector,
                inserter: inserter,
                updater: updater,
                deleter: deleter
            );
        }

        public static void Register<T>
        (
            RESTarMethods method,
            IEnumerable<RESTarMethods> addMethods = null,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null
        ) where T : class
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var attribute = new RESTarAttribute(method, addMethods?.ToArray());
            Resource<T>.Make(attribute, selector, inserter, updater, deleter);
        }
    }
}