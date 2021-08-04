using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources.Operations;
using static System.Reflection.BindingFlags;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// An EntityResourceProvider gives default implementations for the operations of a group of entity resources, 
    /// and defines an attribute that can be used to decorate entity resource types in the application domain. By 
    /// including the ResourceProvider in the call to RESTableConfig.Init(), RESTable can claim entity resource 
    /// types decorated with the attribute and bind the defined resource operations to them.
    /// </summary>
    /// <typeparam name="TBase">The base type for all resources claimed by this ResourceProvider. 
    /// Can be <see cref="object"/> if no such base type exists.</typeparam>
    public abstract class EntityResourceProvider<TBase> : IEntityResourceProviderInternal where TBase : class
    {
        /// <inheritdoc />
        public string Id => GetType().GetEntityResourceProviderId();

        #region IEntityResourceProviderInternal

        #region Helpers

        private void InsertProcedural(IProceduralEntityResource resource, ResourceValidator validator)
        {
            var attribute = new RESTableProceduralAttribute(resource.Methods) { Description = resource.Description };
            var type = resource.Type ?? throw new InvalidOperationException("Could not establish type for procedural resource");
            validator.ValidateRuntimeInsertion(type, resource.Name, attribute);
            validator.Validate(type);
            var inserted = _InsertResource(type, resource.Name, attribute);
            ReceiveClaimed(new[] { inserted });
        }

        private bool RemoveProceduralResource(Type resourceType)
        {
            var iresource = ResourceCollection.SafeGetResource(resourceType);
            if (iresource is null) return true;
            return RemoveResource(iresource);
        }

        TypeCache IEntityResourceProviderInternal.TypeCache
        {
            get => TypeCache;
            set => TypeCache = value;
        }

        ResourceValidator IEntityResourceProviderInternal.ResourceValidator
        {
            get => ResourceValidator;
            set => ResourceValidator = value;
        }

        ResourceCollection IEntityResourceProviderInternal.ResourceCollection
        {
            get => ResourceCollection;
            set => ResourceCollection = value;
        }

        private ResourceValidator ResourceValidator { get; set; } = null!;
        private TypeCache TypeCache { get; set; } = null!;
        private ResourceCollection ResourceCollection { get; set; } = null!;

        private bool RemoveResource(IResource? resource)
        {
            if (resource is IEntityResource er && er.Provider == Id)
            {
                ResourceCollection.RemoveResource(resource);
                return true;
            }
            return false;
        }

        #endregion

        IEnumerable<IProceduralEntityResource> IEntityResourceProviderInternal.SelectProceduralResources(RESTableContext context) => SelectProceduralResources(context);
        bool IEntityResourceProviderInternal.DeleteProceduralResource(RESTableContext context, IProceduralEntityResource resource) => DeleteProceduralResource(context, resource);
        void IEntityResourceProviderInternal.ReceiveClaimed(ICollection<IEntityResource> claimedResources) => ReceiveClaimed(claimedResources);
        void IEntityResourceProviderInternal.ModifyResourceAttribute(Type type, RESTableAttribute attribute) => ModifyResourceAttribute(type, attribute);
        bool IEntityResourceProviderInternal.RemoveProceduralResource(Type resourceType) => RemoveProceduralResource(resourceType);
        void IEntityResourceProviderInternal.InsertProcedural(RESTableContext context, IProceduralEntityResource resource) => InsertProcedural(resource, ResourceValidator);
        bool IEntityResourceProviderInternal.Include(Type type) => Include(type);

        void IEntityResourceProviderInternal.MakeClaimProcedural()
        {
            foreach (var resource in SelectProceduralResources(context: null!))
                InsertProcedural(resource, ResourceValidator);
        }

        void IEntityResourceProviderInternal.Validate() => Validate();

        IProceduralEntityResource IEntityResourceProviderInternal.InsertProceduralResource(RESTableContext context, string n, string d, Method[] m, dynamic data)
        {
            return InsertProceduralResource(context, n, d, m, data);
        }

        void IEntityResourceProviderInternal.SetProceduralResourceMethods(RESTableContext context, IProceduralEntityResource resource, Method[] methods)
        {
            SetProceduralResourceMethods(context, resource, methods);
        }

        void IEntityResourceProviderInternal.SetProceduralResourceDescription(RESTableContext context, IProceduralEntityResource resource, string newDescription)
        {
            SetProceduralResourceDescription(context, resource, newDescription);
        }

        void IEntityResourceProviderInternal.MakeClaimRegular(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var resource = _InsertResource(type);
                if (!IsValid(resource, TypeCache, out var reason))
                    throw new InvalidResourceDeclarationException("An error was found in the declaration for resource " +
                                                                  $"type '{type.GetRESTableTypeName()}': " + reason);
            }
        }

        void IEntityResourceProviderInternal.MakeClaimWrapped(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var resource = _InsertWrapperResource(type, type.GetWrappedType()!);
                if (!IsValid(resource, TypeCache, out var reason))
                    throw new InvalidResourceDeclarationException("An error was found in the declaration for wrapper resource " +
                                                                  $"type '{type.GetRESTableTypeName()}': " + reason);
            }
        }

        #endregion

        #region Internal virtual

        protected virtual bool Include(Type type)
        {
            if (!type.HasAttribute(AttributeType))
                return false;
            if (!typeof(TBase).IsAssignableFrom(type))
                throw new InvalidResourceDeclarationException(
                    $"Invalid resource declaration for type '{type.GetRESTableTypeName()}'. Expected type to " +
                    $"inherit from base type '{typeof(TBase).GetRESTableTypeName()}' as required by resource " +
                    $"provider of type '{GetType().GetRESTableTypeName()}'.");
            return true;
        }

        protected virtual void Validate()
        {
            if (AttributeType is null)
                throw new InvalidEntityResourceProviderException(GetType(), "AttributeType cannot be null");
            if (!AttributeType.IsSubclassOf(typeof(Attribute)))
                throw new InvalidEntityResourceProviderException(GetType(), "Provided AttributeType is not an attribute type");
            if (!AttributeType.IsSubclassOf(typeof(EntityResourceProviderAttribute)))
                throw new InvalidEntityResourceProviderException(GetType(), $"Provided AttributeType '{AttributeType.GetRESTableTypeName()}' " +
                                                                            "does not inherit from RESTable.ResourceProviderAttribute");
        }

        #endregion

        #region Protected

        /// <summary>
        /// The attribute type associated with this ResourceProvider. Used to decorate 
        /// resource types that should be claimed by this ResourceProvider.
        /// </summary>
        protected abstract Type AttributeType { get; }

        /// <summary>
        /// The ReceiveClaimed method is called by RESTable once one or more resources provided
        /// by this ResourceProvider have been added. Override this to provide additional 
        /// logic once resources have been validated and set up.
        /// </summary>
        protected virtual void ReceiveClaimed(ICollection<IEntityResource> claimedResources) { }

        /// <summary>
        /// An optional method for modifying the RESTable resource attribute of a type before the resource is generated
        /// </summary>
        protected virtual void ModifyResourceAttribute(Type type, RESTableAttribute attribute) { }

        /// <summary>
        /// Override this method to add a validation step to the resource claim process. 
        /// </summary>
        /// <param name="resource">The resource to check validity for</param>
        /// <param name="reason">Return the reason for this Type not being valid</param>
        protected virtual bool IsValid(IEntityResource resource, TypeCache typeCache, out string? reason)
        {
            reason = null;
            return true;
        }

        /// <summary>
        /// Returns all procedural entity resources from the provider. Used by RESTable internally. Don't call this method.
        /// </summary>
        protected virtual IEnumerable<IProceduralEntityResource> SelectProceduralResources(RESTableContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new procedural entity resource object with the given name, description and methods. Used by RESTable internally. Don't call this method.
        /// </summary>
        protected virtual IProceduralEntityResource InsertProceduralResource(RESTableContext context, string name, string description, Method[] methods, dynamic data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given update operation. Used by RESTable internally. Don't call this method.
        /// </summary>
        protected virtual void SetProceduralResourceMethods(RESTableContext context, IProceduralEntityResource resource, Method[] methods)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given update operation. Used by RESTable internally. Don't call this method.
        /// </summary>
        protected virtual void SetProceduralResourceDescription(RESTableContext context, IProceduralEntityResource resource, string newDescription)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a dynamic entity resource entity. Used by RESTable internally. Don't call this method.
        /// </summary>
        protected virtual bool DeleteProceduralResource(RESTableContext context, IProceduralEntityResource resource)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Selector to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IEnumerable<T> DefaultSelect<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Inserter to use for resources claimed by this ResourceProvider
        /// </summary>
        ///  <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IEnumerable<T> DefaultInsert<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Updater to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IEnumerable<T> DefaultUpdate<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Deleter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual int DefaultDelete<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Counter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual long DefaultCount<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Selector to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IAsyncEnumerable<T> DefaultSelectAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Inserter to use for resources claimed by this ResourceProvider
        /// </summary>
        ///  <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IAsyncEnumerable<T> DefaultInsertAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Updater to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IAsyncEnumerable<T> DefaultUpdateAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Deleter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual ValueTask<int> DefaultDeleteAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Counter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual ValueTask<long> DefaultCountAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Add resource API

        /// <summary>
        /// Inserts a new resource into the RESTable instance, with the given type, name and attribute.
        /// </summary>
        /// <param name="type">The resource type to insert</param>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        /// <returns></returns>
        private IEntityResource InsertResource(Type type, string? fullName = null, RESTableAttribute? attribute = null)
        {
            ResourceValidator.ValidateRuntimeInsertion(type, fullName, attribute);
            ResourceValidator.Validate(type);
            return _InsertResource(type, fullName, attribute);
        }

        /// <summary>
        /// Inserts a new resource wrapper into the RESTable instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="wrapperType">The wrapper type of the resource</param>
        /// <param name="wrappedType">The type wrapped by the wrapper</param>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        private IEntityResource InsertWrapperResource(Type wrapperType, Type wrappedType, string? fullName = null, RESTableAttribute? attribute = null)
        {
            return _InsertWrapperResource(wrapperType, wrappedType, fullName, attribute);
        }

        /// <summary>
        /// Inserts a new resource into the RESTable instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        /// <typeparam name="TResource">The type to create the resource for</typeparam>
        /// <returns></returns>
        private IEntityResource<TResource> InsertResource<TResource>
        (
            string? fullName = null,
            RESTableAttribute? attribute = null,
            DelegateSet<TResource>? delegates = null
        ) where TResource : class, TBase
        {
            return _InsertResource(fullName, attribute, delegates);
        }

        /// <summary>
        /// Inserts a new resource wrapper into the RESTable instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource's type declaration.</param>
        /// <typeparam name="TWrapper">The resource wrapper type</typeparam>
        /// <typeparam name="TWrapped">The wrapped resource type</typeparam>
        /// <returns></returns>
        private IEntityResource<TWrapped> InsertWrapperResource<TWrapper, TWrapped>
        (
            string? fullName = null,
            RESTableAttribute? attribute = null,
            DelegateSet<TWrapped>? delegates = null
        )
            where TWrapper : ResourceWrapper<TWrapped> where TWrapped : class, TBase
        {
            return _InsertWrapperResource<TWrapper, TWrapped>(fullName, attribute, delegates);
        }

        /// <summary>
        /// Removes the resource corresponding with the given resource type from the RESTable instance
        /// </summary>
        /// <returns>True if and only if a resource was successfully removed</returns>
        private bool RemoveResource<TResource>() where TResource : class, TBase => RemoveResource(ResourceCollection.SafeGetResource<TResource>());

        #endregion

        #region Internals

        private static readonly MethodInfo InsertResourceMethod;
        private static readonly MethodInfo InsertResourceWrappedMethod;

        static EntityResourceProvider()
        {
            var methods = typeof(EntityResourceProvider<TBase>).GetMethods(Instance | NonPublic);
            InsertResourceMethod = methods.First(m => m.Name == nameof(_InsertResource) && m.IsGenericMethod);
            InsertResourceWrappedMethod = methods.First(m => m.Name == nameof(_InsertWrapperResource) && m.IsGenericMethod);
        }

        private IEntityResource _InsertResource(Type type, string? fullName = null, RESTableAttribute? attribute = null)
        {
            var method = InsertResourceMethod.MakeGenericMethod(type);
            var entityResource = (IEntityResource?)method.Invoke(this, new object?[] { fullName, attribute, null });
            return entityResource!;
        }

        private IEntityResource _InsertWrapperResource(Type wrapperType, Type wrappedType, string? fullName = null, RESTableAttribute? attribute = null)
        {
            var method = InsertResourceWrappedMethod.MakeGenericMethod(wrapperType, wrappedType);
            var entityResource = (IEntityResource?)method.Invoke(this, new object?[] { fullName, attribute, null });
            return entityResource!;
        }

        private IEntityResource<TResource> _InsertResource<TResource>
        (
            string? fullName = null,
            RESTableAttribute? attribute = null,
            DelegateSet<TResource>? delegates = null
        ) where TResource : class, TBase => new EntityResource<TResource>
        (
            fullName: fullName ?? typeof(TResource).GetRESTableTypeName(),
            attribute: attribute ?? typeof(TResource).GetCustomAttribute<RESTableAttribute>() ?? throw new Exception("Could not get RESTableAttribute from resource type"),
            delegates: ResolveDelegateSet<TResource, TResource>(delegates),
            views: GetViews<TResource>(),
            provider: this,
            typeCache: TypeCache,
            resourceCollection: ResourceCollection
        );

        private IEntityResource<TWrapped> _InsertWrapperResource<TWrapper, TWrapped>
        (
            string? fullName = null,
            RESTableAttribute? attribute = null,
            DelegateSet<TWrapped>? delegates = null
        )
            where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => new EntityResource<TWrapped>
        (
            fullName: fullName ?? typeof(TWrapper).GetRESTableTypeName(),
            attribute: attribute ?? typeof(TWrapper).GetCustomAttribute<RESTableAttribute>() ?? throw new Exception("Could not get RESTableAttribute from resource type"),
            delegates: ResolveDelegateSet<TWrapper, TWrapped>(delegates),
            views: GetWrappedViews<TWrapper, TWrapped>(),
            provider: this,
            typeCache: TypeCache,
            resourceCollection: ResourceCollection
        );

        private DelegateSet<TResource> ResolveDelegateSet<TTarget, TResource>(DelegateSet<TResource>? delegates)
            where TResource : class, TBase
            where TTarget : class => (delegates ?? new DelegateSet<TResource>())
            .GetDelegatesFromTargetWhereNull<TTarget>()
            .SetDelegatesToDefaultsWhereNull
            (
                selector: DefaultSelect, asyncSelector: DefaultSelectAsync,
                inserter: DefaultInsert, asyncInserter: DefaultInsertAsync,
                updater: DefaultUpdate, asyncUpdater: DefaultUpdateAsync,
                deleter: DefaultDelete, asyncDeleter: DefaultDeleteAsync,
                counter: DefaultCount, asyncCounter: DefaultCountAsync
            )
            .SetDelegatesToNullWhereNotImplemented()
            .SetAsyncDelegatesToSyncWhereNull();

        private View<TResource>[] GetViews<TResource>() where TResource : class, TBase => typeof(TResource)
            .GetNestedTypes()
            .Where(nested => nested.HasAttribute<RESTableViewAttribute>())
            .Select(view => new View<TResource>(view, TypeCache))
            .ToArray();

        private View<TWrapped>[] GetWrappedViews<TWrapper, TWrapped>() where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => typeof(TWrapper)
            .GetNestedTypes()
            .Where(nested => nested.HasAttribute<RESTableViewAttribute>())
            .Select(view => new View<TWrapped>(view, TypeCache))
            .ToArray();

        #endregion
    }
}