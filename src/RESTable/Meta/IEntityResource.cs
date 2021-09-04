using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    public interface IEntityResource<TResource> : IResource<TResource>, IEntityResource where TResource : class
    {
        /// <summary>
        /// The Views registered for this resource
        /// </summary>
        IReadOnlyDictionary<string, ITarget<TResource>> ViewDictionary { get; }

        bool CanSelect { get; }
        bool CanInsert { get; }
        bool CanUpdate { get; }
        bool CanDelete { get; }
        bool CanCount { get; }

        IAsyncEnumerable<TResource> InsertAsync(IRequest<TResource> request, CancellationToken cancellationToken = new());
        IAsyncEnumerable<TResource> UpdateAsync(IRequest<TResource> request, CancellationToken cancellationToken = new());
        ValueTask<long> DeleteAsync(IRequest<TResource> request, CancellationToken cancellationToken = new());
        ValueTask<AuthResults> AuthenticateAsync(IRequest<TResource> request, CancellationToken cancellationToken = new());
        ValueTask<long> CountAsync(IRequest<TResource> request, CancellationToken cancellationToken = new());
        IAsyncEnumerable<TResource> Validate(IAsyncEnumerable<TResource> entities, RESTableContext context, CancellationToken cancellationToken = new());
    }
}