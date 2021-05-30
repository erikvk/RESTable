using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RESTable.Admin;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources.Operations;

namespace RESTable.Resources
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IAsyncInserter{T}" />
    /// <inheritdoc cref="IAsyncUpdater{T}" />
    /// <inheritdoc cref="IAsyncDeleter{T}" />
    /// <inheritdoc cref="ResourceController{TController,TProvider}" />
    /// <summary>
    /// Resource controllers attach to entity resource providers that support procedural resources,
    /// and enable insertion of resources during runtime.
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    /// <typeparam name="TController"></typeparam>
    public abstract class ResourceController<TController, TProvider> :
        Resource,
        ISelector<TController>,
        IAsyncInserter<TController>,
        IAsyncUpdater<TController>,
        IAsyncDeleter<TController>
        where TController : ResourceController<TController, TProvider>, new()
        where TProvider : IEntityResourceProvider, IProceduralEntityResourceProvider
    {
        internal static string BaseNamespace { private get; set; }
        internal static TProvider ResourceProvider { private get; set; }

        private static IEntityResourceProviderInternal ResourceProviderInternal => (IEntityResourceProviderInternal) ResourceProvider;

        private void ResolveDynamicResourceName(ref string name)
        {
            switch (name)
            {
                case var _ when !Regex.IsMatch(name, RegEx.DynamicResourceName):
                    throw new Exception($"Resource name '{name}' contains invalid characters. Letters, nu" +
                                        "mbers and underscores are valid in resource names. Dots can be used " +
                                        "to organize resources into namespaces. No other characters can be used.");
                case var _ when name.StartsWith(".") || name.Contains("..") || name.EndsWith("."):
                    throw new Exception($"'{name}' is not a valid resource name. Invalid namespace syntax");
            }
            if (!name.StartsWith($"{BaseNamespace}."))
            {
                if (name.StartsWith($"{BaseNamespace}.", StringComparison.OrdinalIgnoreCase))
                {
                    var nrOfDots = name.Count(c => c == '.') + 2;
                    name = $"{BaseNamespace}.{name.Split(new[] {'.'}, nrOfDots).Last()}";
                }
                else name = $"{BaseNamespace}.{name}";
            }
            if (ResourceProviderInternal.ResourceCollection.TryGetResource(name, out _))
                throw new Exception($"Invalid resource name '{name}'. Name already in use.");
        }

        /// <summary>
        /// Additional data associated with this resource (as defined by the resource provider)
        /// </summary>
        [RESTableMember(ignore: true)]
        protected virtual dynamic Data => null;

        /// <summary>
        /// Selects the instances that have been inserted by this controller
        /// </summary>
        protected static IEnumerable<TController> Select(RESTableContext context) => ResourceProviderInternal
            .SelectProceduralResources(context)
            .OrderBy(r => r.Name)
            .Select(r => Make<TController>(ResourceProviderInternal.ResourceCollection.SafeGetResource(r.Name)));

        /// <summary>
        /// Inserts the current instance as a new procedural resource
        /// </summary>
        protected void Insert(RESTableContext context)
        {
            var name = Name;
            var methods = EnabledMethods;
            var description = Description;
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Missing or invalid name for new resource");
            ResolveDynamicResourceName(ref name);
            var methodsArray = methods.ResolveMethodRestrictions().ToArray();

            var inserted =
                ResourceProviderInternal.InsertProceduralResource(context, name, description, methodsArray, (object) Data);
            if (inserted is not null)
                ResourceProviderInternal.InsertProcedural(context, inserted);
        }

        /// <summary>
        /// Updates the state of the current instance to the corresponding procedural resource
        /// </summary>
        protected void Update(RESTableContext context)
        {
            var procedural = ResourceProviderInternal.SelectProceduralResources(context)
                                 ?.FirstOrDefault(item => item.Name == Name) ??
                             throw new InvalidOperationException(
                                 $"Cannot update resource '{Name}'. Resource has not been inserted.");
            var resource = (IResourceInternal) ResourceProviderInternal.ResourceCollection.SafeGetResource(procedural.Type) ??
                           throw new InvalidOperationException(
                               $"Cannot update resource '{Name}'. Resource has not been inserted.");
            ResourceProviderInternal.SetProceduralResourceDescription(context, procedural, Description);
            resource.Description = Description;
            var methods = EnabledMethods.ResolveMethodRestrictions().ToArray();
            ResourceProviderInternal.SetProceduralResourceMethods(context, procedural, methods);
            resource.AvailableMethods = methods;
        }

        /// <summary>
        /// Deletes the corresponding procedural resource
        /// </summary>
        protected void Delete(RESTableContext context)
        {
            var procedural = ResourceProviderInternal.SelectProceduralResources(context)
                ?.FirstOrDefault(item => item.Name == Name);
            if (procedural is null) return;
            var type = procedural.Type;
            if (ResourceProviderInternal.DeleteProceduralResource(context, procedural))
                ResourceProviderInternal.RemoveProceduralResource(type);
        }

        #region RESTable resource methods

        /// <inheritdoc />
        public virtual IEnumerable<TController> Select(IRequest<TController> request) => Select(request.Context);

        public virtual async IAsyncEnumerable<TController> InsertAsync(IRequest<TController> request)
        {
            await foreach (var resource in request.GetInputEntitiesAsync().ConfigureAwait(false))
            {
                resource.Insert(request.Context);
                yield return resource;
            }
        }

        public virtual async IAsyncEnumerable<TController> UpdateAsync(IRequest<TController> request)
        {
            await foreach (var resource in request.GetInputEntitiesAsync().ConfigureAwait(false))
            {
                resource.Update(request.Context);
                yield return resource;
            }
        }

        public virtual async ValueTask<int> DeleteAsync(IRequest<TController> request)
        {
            var i = 0;
            await foreach (var resource in request.GetInputEntitiesAsync().ConfigureAwait(false))
            {
                resource.Delete(request.Context);
                i += 1;
            }
            return i;
        }

        #endregion
    }
}