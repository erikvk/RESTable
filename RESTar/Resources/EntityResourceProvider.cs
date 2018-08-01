using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.Resources.Operations.DelegateMaker;
using IResource = RESTar.Meta.IResource;
using Resource = RESTar.Meta.Resource;

namespace RESTar.Resources
{
    /// <summary>
    /// A common abstract class for generic EntityResourceProvider instances
    /// </summary>
    public abstract class EntityResourceProvider
    {
        internal abstract bool Include(Type type);
        internal abstract void MakeClaimRegular(IEnumerable<Type> types);
        internal abstract void MakeClaimWrapped(IEnumerable<Type> types);
        internal abstract void MakeClaimProcedural();
        internal abstract void InsertProcedural(IProceduralEntityResource resource);
        internal abstract void Validate();
        internal EntityResourceProvider() { }
        internal ICollection<Type> GetClaim(IEnumerable<Type> types) => types.Where(Include).ToList();

        internal IEnumerable<IProceduralEntityResource> _Select() => SelectProceduralResources();

        internal void _Insert(string name, string description, Method[] methods, dynamic data)
        {
            var inserted = InsertProceduralResource(name, description, methods, data);
            if (inserted == null) return;
            InsertProcedural(inserted);
        }

        internal void _SetMethods(IProceduralEntityResource procedural, IResourceInternal resource, IReadOnlyList<Method> methods)
        {
            SetProceduralResourceMethods(procedural, methods.ToArray());
            resource.AvailableMethods = methods;
        }

        internal void _SetDescription(IProceduralEntityResource procedural, IResourceInternal resource, string description)
        {
            SetProceduralResourceDescription(procedural, description);
            resource.Description = description;
        }

        internal void _Delete(IProceduralEntityResource procedural)
        {
            if (procedural == null) return;
            var type = procedural.Type;
            Db.TransactAsync(() => ResourceAlias.GetByResource(procedural.Name)?.Delete());
            if (DeleteProceduralResource(procedural))
                RemoveProceduralResource(type);
        }

        /// <summary>
        /// The attribute type associated with this ResourceProvider. Used to decorate 
        /// resource types that should be claimed by this ResourceProvider.
        /// </summary>
        protected abstract Type AttributeType { get; }

        /// <summary>
        /// Returns all procedural entity resources from the provider. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual IEnumerable<IProceduralEntityResource> SelectProceduralResources()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new dynamic entity resource object with the given name, description and methods. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods, dynamic data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given update operation. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given update operation. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a dynamic entity resource entity. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual bool DeleteProceduralResource(IProceduralEntityResource resource)
        {
            throw new NotImplementedException();
        }

        internal IDatabaseIndexer _DatabaseIndexer => DatabaseIndexer;

        /// <summary>
        /// IDatabaseIndexers are plugins for the DatabaseIndex resource, that allow resources 
        /// created by this provider to have database indexes managed by that resource.
        /// </summary>
        protected virtual IDatabaseIndexer DatabaseIndexer { get; } = null;

        internal virtual void _ReceiveClaimed(ICollection<IEntityResource> claimedResources) => ReceiveClaimed(claimedResources);

        /// <summary>
        /// The ReceiveClaimed method is called by RESTar once one or more resources provided
        /// by this ResourceProvider have been added. Override this to provide additional 
        /// logic once resources have been validated and set up.
        /// </summary>
        protected virtual void ReceiveClaimed(ICollection<IEntityResource> claimedResources) { }

        internal void _ModifyResourceAttribute(Type type, RESTarAttribute attribute) => ModifyResourceAttribute(type, attribute);

        /// <summary>
        /// An optional method for modifying the RESTar resource attribute of a type before the resource is generated
        /// </summary>
        protected virtual void ModifyResourceAttribute(Type type, RESTarAttribute attribute) { }

        /// <summary>
        /// Override this method to add a validation step to the resource claim process. 
        /// </summary>
        /// <param name="resource">The resource to check validity for</param>
        /// <param name="reason">Return the reason for this Type not being valid</param>
        protected virtual bool IsValid(IEntityResource resource, out string reason)
        {
            reason = null;
            return true;
        }

        internal bool RemoveProceduralResource(Type resourceType)
        {
            var iresource = Resource.SafeGet(resourceType);
            if (iresource == null) return true;
            return RemoveResource(iresource);
        }

        /// <summary>
        /// Removes the given resource from the RESTar instance
        /// </summary>
        /// <returns>True if and only if a resource was successfully removed</returns>
        protected bool RemoveResource(IResource resource)
        {
            if (resource is IEntityResource er && er.Provider == this.GetProviderId())
            {
                RESTarConfig.RemoveResource(resource);
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// An EntityResourceProvider gives default implementations for the operations of a group of entity resources, 
    /// and defines an attribute that can be used to decorate entity resource types in the application domain. By 
    /// including the ResourceProvider in the call to RESTarConfig.Init(), RESTar can claim entity resource 
    /// types decorated with the attribute and bind the defined resource operations to them.
    /// </summary>
    /// <typeparam name="TBase">The base type for all resources claimed by this ResourceProvider. 
    /// Can be <see cref="object"/> if no such base type exists.</typeparam>
    public abstract class EntityResourceProvider<TBase> : EntityResourceProvider where TBase : class
    {
        #region Public members

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
        protected virtual int DefaultInsert<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Updater to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual int DefaultUpdate<T>(IRequest<T> request) where T : class, TBase
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
        /// The default Profiler to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual ResourceProfile DefaultProfile<T>(IEntityResource<T> resource) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        #region Add resource API

        /// <summary>
        /// Inserts a new resource into the RESTar instance, with the given type, name and attribute.
        /// </summary>
        /// <param name="type">The resource type to insert</param>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        /// <returns></returns>
        private IEntityResource InsertResource(Type type, string fullName = null, RESTarAttribute attribute = null)
        {
            ResourceValidator.ValidateRuntimeInsertion(type, fullName, attribute);
            ResourceValidator.Validate(type);
            return _InsertResource(type, fullName, attribute);
        }

        /// <summary>
        /// Inserts a new resource wrapper into the RESTar instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="wrapperType">The wrapper type of the resource</param>
        /// <param name="wrappedType">The type wrapped by the wrapper</param>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        private IEntityResource InsertWrapperResource(Type wrapperType, Type wrappedType, string fullName = null, RESTarAttribute attribute = null)
        {
            return _InsertWrapperResource(wrapperType, wrappedType, fullName, attribute);
        }

        /// <summary>
        /// Inserts a new resource into the RESTar instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        /// <param name="selector">The selector to use. If null, the default selector is used</param>
        /// <param name="inserter">The inserter to use. If null, the default inserter is used</param>
        /// <param name="updater">The updater to use. If null, the default updater is used</param>
        /// <param name="deleter">The deleter to use. If null, the default deleter is used</param>
        /// <param name="counter">The counter to use. If null, the default counter is used</param>
        /// <param name="profiler">The profiler to use. If null, the default profiler is used</param>
        /// <param name="authenticator">The authenticator to use. If null, the default authenticator is used</param>
        /// <typeparam name="TResource">The type to create the resource for</typeparam>
        /// <returns></returns>
        private IEntityResource<TResource> InsertResource<TResource>(string fullName = null, RESTarAttribute attribute = null,
            Selector<TResource> selector = null, Inserter<TResource> inserter = null, Updater<TResource> updater = null,
            Deleter<TResource> deleter = null, Counter<TResource> counter = null, Profiler<TResource> profiler = null,
            Authenticator<TResource> authenticator = null) where TResource : class, TBase
        {
            return _InsertResource(fullName, attribute, selector, inserter, updater, deleter, counter, profiler, authenticator);
        }

        /// <summary>
        /// Inserts a new resource wrapper into the RESTar instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource's type declaration.</param>
        /// <param name="selector">The selector to use. If null, the default selector is used</param>
        /// <param name="inserter">The inserter to use. If null, the default inserter is used</param>
        /// <param name="updater">The updater to use. If null, the default updater is used</param>
        /// <param name="deleter">The deleter to use. If null, the default deleter is used</param>
        /// <param name="counter">The counter to use. If null, the default counter is used</param>
        /// <param name="profiler">The profiler to use. If null, the default profiler is used</param>
        /// <param name="authenticator">The authenticator to use. If null, the default authenticator is used</param>
        /// <typeparam name="TWrapper">The resource wrapper type</typeparam>
        /// <typeparam name="TWrapped">The wrapped resource type</typeparam>
        /// <returns></returns>
        private IEntityResource<TWrapped> InsertWrapperResource<TWrapper, TWrapped>(string fullName = null, RESTarAttribute attribute = null,
            Selector<TWrapped> selector = null, Inserter<TWrapped> inserter = null, Updater<TWrapped> updater = null, Deleter<TWrapped> deleter = null,
            Counter<TWrapped> counter = null, Profiler<TWrapped> profiler = null, Authenticator<TWrapped> authenticator = null)
            where TWrapper : ResourceWrapper<TWrapped> where TWrapped : class, TBase
        {
            return _InsertWrapperResource<TWrapper, TWrapped>(fullName, attribute, selector, inserter, updater, deleter, counter, profiler,
                authenticator);
        }

        /// <summary>
        /// Removes the resource corresponding with the given resource type from the RESTar instance
        /// </summary>
        /// <returns>True if and only if a resource was successfully removed</returns>
        private bool RemoveResource<TResource>() where TResource : class, TBase => RemoveResource(Resource<TResource>.SafeGet);

        #endregion

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


        private IEntityResource _InsertResource(Type type, string fullName = null, RESTarAttribute attribute = null)
        {
            var method = InsertResourceMethod.MakeGenericMethod(type);
            return (IEntityResource) method.Invoke(this, new object[] {fullName, attribute, null, null, null, null, null, null, null});
        }

        private IEntityResource _InsertWrapperResource(Type wrapperType, Type wrappedType, string fullName = null, RESTarAttribute attribute = null)
        {
            var method = InsertResourceWrappedMethod.MakeGenericMethod(wrapperType, wrappedType);
            return (IEntityResource) method.Invoke(this, new object[] {fullName, attribute, null, null, null, null, null, null, null});
        }

        private IEntityResource<TResource> _InsertResource<TResource>(
            string fullName = null,
            RESTarAttribute attribute = null,
            Selector<TResource> selector = null,
            Inserter<TResource> inserter = null,
            Updater<TResource> updater = null,
            Deleter<TResource> deleter = null,
            Counter<TResource> counter = null,
            Profiler<TResource> profiler = null,
            Authenticator<TResource> authenticator = null
        ) where TResource : class, TBase
        {
            return new Meta.Internal.EntityResource<TResource>
            (
                fullName: fullName ?? typeof(TResource).RESTarTypeName(),
                attribute: attribute ?? typeof(TResource).GetCustomAttribute<RESTarAttribute>(),
                selector: selector ?? GetDelegate<Selector<TResource>>(typeof(TResource)) ?? DefaultSelect,
                inserter: inserter ?? GetDelegate<Inserter<TResource>>(typeof(TResource)) ?? DefaultInsert,
                updater: updater ?? GetDelegate<Updater<TResource>>(typeof(TResource)) ?? DefaultUpdate,
                deleter: deleter ?? GetDelegate<Deleter<TResource>>(typeof(TResource)) ?? DefaultDelete,
                counter: counter ?? GetDelegate<Counter<TResource>>(typeof(TResource)) ?? DefaultCount,
                profiler: profiler ?? DefaultProfile,
                authenticator: authenticator ?? GetDelegate<Authenticator<TResource>>(typeof(TResource)),
                views: GetViews<TResource>(),
                provider: this
            );
        }

        private IEntityResource<TWrapped> _InsertWrapperResource<TWrapper, TWrapped>(
            string fullName = null,
            RESTarAttribute attribute = null,
            Selector<TWrapped> selector = null,
            Inserter<TWrapped> inserter = null,
            Updater<TWrapped> updater = null,
            Deleter<TWrapped> deleter = null,
            Counter<TWrapped> counter = null,
            Profiler<TWrapped> profiler = null,
            Authenticator<TWrapped> authenticator = null
        )
            where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => new Meta.Internal.EntityResource<TWrapped>
        (
            fullName: fullName ?? typeof(TWrapper).RESTarTypeName(),
            attribute: attribute ?? typeof(TWrapper).GetCustomAttribute<RESTarAttribute>(),
            selector: selector ?? GetDelegate<Selector<TWrapped>>(typeof(TWrapper)) ?? DefaultSelect,
            inserter: inserter ?? GetDelegate<Inserter<TWrapped>>(typeof(TWrapper)) ?? DefaultInsert,
            updater: updater ?? GetDelegate<Updater<TWrapped>>(typeof(TWrapper)) ?? DefaultUpdate,
            deleter: deleter ?? GetDelegate<Deleter<TWrapped>>(typeof(TWrapper)) ?? DefaultDelete,
            counter: counter ?? GetDelegate<Counter<TWrapped>>(typeof(TWrapper)) ?? DefaultCount,
            profiler: profiler ?? DefaultProfile,
            authenticator: authenticator ?? GetDelegate<Authenticator<TWrapped>>(typeof(TWrapper)),
            views: GetWrappedViews<TWrapper, TWrapped>(),
            provider: this
        );

        internal override void InsertProcedural(IProceduralEntityResource resource)
        {
            var attribute = new RESTarProceduralAttribute(resource.Methods) {Description = resource.Description};
            var type = resource.Type;
            ResourceValidator.ValidateRuntimeInsertion(type, resource.Name, attribute);
            ResourceValidator.Validate(type);
            var inserted = _InsertResource(type, resource.Name, attribute);
            ReceiveClaimed(new[] {inserted});
        }

        internal override bool Include(Type type)
        {
            if (!typeof(TBase).IsAssignableFrom(type))
                throw new InvalidResourceDeclarationException(
                    $"Invalid resource declaration for type '{type.RESTarTypeName()}'. Expected type to " +
                    $"inherit from base type '{typeof(TBase).RESTarTypeName()}' as required by resource " +
                    $"provider of type '{GetType().RESTarTypeName()}'.");
            return type.HasAttribute(AttributeType);
        }

        internal override void MakeClaimProcedural() => SelectProceduralResources().ForEach(InsertProcedural);

        internal override void MakeClaimRegular(IEnumerable<Type> types) => types.ForEach(type =>
        {
            var resource = _InsertResource(type);
            if (!IsValid(resource, out var reason))
                throw new InvalidResourceDeclarationException("An error was found in the declaration for resource " +
                                                              $"type '{type.RESTarTypeName()}': " + reason);
        });

        internal override void MakeClaimWrapped(IEnumerable<Type> types) => types.ForEach(type =>
        {
            var resource = _InsertWrapperResource(type, type.GetWrappedType());
            if (!IsValid(resource, out var reason))
                throw new InvalidResourceDeclarationException("An error was found in the declaration for wrapper resource " +
                                                              $"type '{type.RESTarTypeName()}': " + reason);
        });

        internal override void Validate()
        {
            if (AttributeType == null)
                throw new InvalidExternalResourceProviderException("AttributeType cannot be null");
            if (!AttributeType.IsSubclassOf(typeof(Attribute)))
                throw new InvalidExternalResourceProviderException("Provided AttributeType is not an attribute type");
            if (!AttributeType.IsSubclassOf(typeof(EntityResourceProviderAttribute)))
                throw new InvalidExternalResourceProviderException($"Provided AttributeType '{AttributeType.RESTarTypeName()}' " +
                                                                   "does not inherit from RESTar.ResourceProviderAttribute");
        }

        private static View<TResource>[] GetViews<TResource>() where TResource : class, TBase => typeof(TResource)
            .GetNestedTypes()
            .Where(nested => nested.HasAttribute<RESTarViewAttribute>())
            .Select(view => new View<TResource>(view))
            .ToArray();

        private static View<TWrapped>[] GetWrappedViews<TWrapper, TWrapped>() where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => typeof(TWrapper)
            .GetNestedTypes()
            .Where(nested => nested.HasAttribute<RESTarViewAttribute>())
            .Select(view => new View<TWrapped>(view))
            .ToArray();

        #endregion
    }
}