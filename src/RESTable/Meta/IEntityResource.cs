using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta
{
    /// <inheritdoc cref="ITarget" />
    /// <summary>
    /// The common non-generic interface for all entity resource entities used by RESTable
    /// </summary>
    public interface IEntityResource : IResource
    {
        /// <summary>
        /// The resource provider that generated this resource
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Returns true if and only if this resource was claimed by the given 
        /// ResourceProvider type
        /// </summary>
        bool ClaimedBy<T>() where T : IEntityResourceProvider;

        /// <summary>
        /// Is this a DDictionary resource?
        /// </summary>
        bool IsDDictionary { get; }

        /// <summary>
        /// Does this resource contain dynamic members?
        /// </summary>
        bool IsDynamic { get; }

        /// <summary>
        /// Does this entity resource have a type declaration?
        /// </summary>
        bool IsDeclared { get; }

        /// <summary>
        /// Are runtime-defined conditions allowed in requests to this resource?
        /// </summary>
        bool DynamicConditionsAllowed { get; }

        /// <summary>
        /// Are the public instance properties defined in this resource's type 
        /// flagged (preceded by $) in the REST API to avoid capture against 
        /// dynamic properties?
        /// </summary>
        bool DeclaredPropertiesFlagged { get; }

        /// <summary>
        /// The binding rule to use when binding output terms for this resource
        /// </summary>
        TermBindingRule OutputBindingRule { get; }

        /// <summary>
        /// Does this resource require validation on insertion and updating?
        /// </summary>
        bool RequiresValidation { get; }

        
        /// <summary>
        /// The views for this resource
        /// </summary>
        IEnumerable<ITarget> Views { get; }

        /// <summary>
        /// Does this resource have a separate authentication
        /// for REST requests?
        /// </summary>
        bool RequiresAuthentication { get; }
    }

    /// <inheritdoc cref="ITarget{T}" />
    /// <summary>
    /// The common generic interface for all entity resources used by RESTable
    /// </summary>
    public interface IEntityResource<T> : IResource<T>, IEntityResource where T : class
    {
        /// <summary>
        /// RESTable inserter (don't call)
        /// </summary>
        int Insert(IRequest<T> request);

        /// <summary>
        /// RESTable updater (don't call)
        /// </summary>
        int Update(IRequest<T> request);

        /// <summary>
        /// RESTable deleter (don't call)
        /// </summary>
        int Delete(IRequest<T> request);

        /// <summary>
        /// RESTable authenticator (don't call)
        /// </summary>
        AuthResults Authenticate(IRequest<T> request);

        /// <summary>
        /// RESTable counter (don't call)
        /// </summary>
        long Count(IRequest<T> request);

        /// <summary>
        /// Runs resource-specific validation on an <see cref="IEnumerable{T}"/>
        /// and throws a FailedValidation if any entity failed validation.
        /// </summary>
        IEnumerable<T> Validate(IEnumerable<T> entities);

        /// <summary>
        /// The Views registered for this resource
        /// </summary>
        IReadOnlyDictionary<string, ITarget<T>> ViewDictionary { get; }

        /// <summary>
        /// Can this resource select entities?
        /// </summary>
        bool CanSelect { get; }

        /// <summary>
        /// Can this resource insert entities?
        /// </summary>
        bool CanInsert { get; }

        /// <summary>
        /// Can this resource update entities?
        /// </summary>
        bool CanUpdate { get; }

        /// <summary>
        /// Can this resource delete entities?
        /// </summary>
        bool CanDelete { get; }

        /// <summary>
        /// Can this resource delete entities?
        /// </summary>
        bool CanCount { get; }
    }
}