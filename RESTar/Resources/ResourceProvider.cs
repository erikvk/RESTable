using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using static System.Reflection.BindingFlags;

namespace RESTar.Resources
{
    /// <summary>
    /// A common abstract class for generic ResourceProvider instances
    /// </summary>
    public abstract class ResourceProvider
    {
        internal abstract bool Include(Type type);
        internal abstract void MakeClaimRegular(IEnumerable<Type> types);
        internal abstract void MakeClaimWrapped(IEnumerable<Type> types);
        internal abstract void Validate();

        /// <summary>
        /// IndexProviders are plugins for the DatabaseIndex resource, that allow resources 
        /// created by this provider to have database indexes managed by that resource.
        /// </summary>
        public IDatabaseIndexer DatabaseIndexer { get; set; }

        /// <summary>
        /// The ReceiveClaimed method is called by RESTar once the resources provided
        /// by this ResourceProvider have been added.
        /// </summary>
        public virtual void ReceiveClaimed(ICollection<IResource> claimedResources)
        {
        }

        internal ResourceProvider()
        {
        }

        internal ICollection<Type> GetClaim(IEnumerable<Type> types) => types.Where(Include).ToList();
    }

    /// <inheritdoc />
    /// <summary>
    /// A ResourceProvider gives default implementations for the operations of a group of resources, 
    /// and defines an attribute that can be used to decorate resource types in the application domain. By 
    /// including the ResourceProvider in the call to RESTarConfig.Init(), RESTar can claim resource 
    /// types decorated with the attribute and bind the defined resource operations to them.
    /// </summary>
    /// <typeparam name="TBase">The base type for all resources claimed by this ResourceProvider. 
    /// Can be 'object' if no such base type exists.</typeparam>
    public abstract class ResourceProvider<TBase> : ResourceProvider where TBase : class
    {
        #region Public members

        /// <summary>
        /// The attribute type associated with this ResourceProvider. Used to decorate 
        /// resource types that should be claimed by this ResourceProvider.
        /// </summary>
        public abstract Type AttributeType { get; }

        /// <inheritdoc />
        public ResourceProvider()
        {
        }

        /// <summary>
        /// The default Selector to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public abstract Selector<T> GetDefaultSelector<T>() where T : class, TBase;

        /// <summary>
        /// The default Inserter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public abstract Inserter<T> GetDefaultInserter<T>() where T : class, TBase;

        /// <summary>
        /// The default Updater to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public abstract Updater<T> GetDefaultUpdater<T>() where T : class, TBase;

        /// <summary>
        /// The default Deleter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public abstract Deleter<T> GetDefaultDeleter<T>() where T : class, TBase;

        /// <summary>
        /// The default Counter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public abstract Counter<T> GetDefaultCounter<T>() where T : class, TBase;

        /// <summary>
        /// The default Profiler to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public abstract Profiler<T> GetProfiler<T>() where T : class, TBase;

        /// <summary>
        /// Override this method to add a validation step to the resource claim process. 
        /// This validation will be run for all claimed types before their resources are 
        /// created.
        /// </summary>
        /// <param name="type">The type to check validity for</param>
        /// <param name="reason">Return the reason for this Type not being valid</param>
        /// <returns>True if and only if the resource type is valid</returns>
        public virtual bool IsValid(Type type, out string reason)
        {
            reason = null;
            return true;
        }

        #endregion

        #region Internals

        internal override bool Include(Type type)
        {
            if (!typeof(TBase).IsAssignableFrom(type))
                throw new ResourceDeclarationException(
                    $"Invalid resource declaration for type '{type.FullName}'. Expected type to " +
                    $"inherit from base type '{typeof(TBase).FullName}' as required by resource " +
                    $"provider of type '{GetType().FullName}'.");
            return type.HasAttribute(AttributeType);
        }

        internal static readonly MethodInfo BuildRegularMethod;
        internal static readonly MethodInfo BuildWrapperMethod;

        static ResourceProvider()
        {
            BuildRegularMethod = typeof(ResourceProvider<TBase>).GetMethod(nameof(BuildRegularResource), Instance | NonPublic);
            BuildWrapperMethod = typeof(ResourceProvider<TBase>).GetMethod(nameof(BuildWrapperResource), Instance | NonPublic);
        }

        internal override void MakeClaimRegular(IEnumerable<Type> types) => types.ForEach(type =>
        {
            if (!IsValid(type, out var reason))
                throw new ResourceDeclarationException("An error was found in the declaration for resource " +
                                                       $"type '{type.FullName}': " + reason);
            BuildRegularMethod.MakeGenericMethod(type).Invoke(this, null);
        });

        internal override void MakeClaimWrapped(IEnumerable<Type> types) => types.ForEach(type =>
        {
            if (!IsValid(type, out var reason))
                throw new ResourceDeclarationException("An error was found in the declaration for wrapper resource " +
                                                       $"type '{type.FullName}': " + reason);
            BuildWrapperMethod.MakeGenericMethod(type, type.GetWrappedType()).Invoke(this, null);
        });

        private void BuildRegularResource<TResource>() where TResource : class, TBase => new Internal.Resource<TResource>
        (
            name: typeof(TResource).FullName,
            attribute: typeof(TResource).GetAttribute<RESTarAttribute>(),
            selector: DelegateMaker.GetDelegate<Selector<TResource>, TResource>() ?? GetDefaultSelector<TResource>(),
            inserter: DelegateMaker.GetDelegate<Inserter<TResource>, TResource>() ?? GetDefaultInserter<TResource>(),
            updater: DelegateMaker.GetDelegate<Updater<TResource>, TResource>() ?? GetDefaultUpdater<TResource>(),
            deleter: DelegateMaker.GetDelegate<Deleter<TResource>, TResource>() ?? GetDefaultDeleter<TResource>(),
            counter: DelegateMaker.GetDelegate<Counter<TResource>, TResource>() ?? GetDefaultCounter<TResource>(),
            profiler: GetProfiler<TResource>(),
            provider: this
        );

        private void BuildWrapperResource<TWrapper, TWrapped>()
            where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => new Internal.Resource<TWrapped>
        (
            name: typeof(TWrapper).FullName,
            attribute: typeof(TWrapper).GetAttribute<RESTarAttribute>(),
            selector: DelegateMaker.GetDelegate<Selector<TWrapped>, TWrapper, TWrapped>() ?? GetDefaultSelector<TWrapped>(),
            inserter: DelegateMaker.GetDelegate<Inserter<TWrapped>, TWrapper, TWrapped>() ?? GetDefaultInserter<TWrapped>(),
            updater: DelegateMaker.GetDelegate<Updater<TWrapped>, TWrapper, TWrapped>() ?? GetDefaultUpdater<TWrapped>(),
            deleter: DelegateMaker.GetDelegate<Deleter<TWrapped>, TWrapper, TWrapped>() ?? GetDefaultDeleter<TWrapped>(),
            counter: DelegateMaker.GetDelegate<Counter<TWrapped>, TWrapper, TWrapped>() ?? GetDefaultCounter<TWrapped>(),
            profiler: GetProfiler<TWrapped>(),
            provider: this
        );

        internal override void Validate()
        {
            if (AttributeType == null)
                throw new ExternalResourceProviderException("AttributeType cannot be null");
            if (!AttributeType.IsSubclassOf(typeof(Attribute)))
                throw new ExternalResourceProviderException("Provided AttributeType is not an attribute type");
            if (!AttributeType.IsSubclassOf(typeof(ResourceProviderAttribute)))
                throw new ExternalResourceProviderException($"Provided AttributeType '{AttributeType.FullName}' " +
                                                            $"does not inherit from RESTar.ResourceProviderAttribute");
        }

        #endregion
    }
}