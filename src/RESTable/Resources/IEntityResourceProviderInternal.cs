using System;
using System.Collections.Generic;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// A common abstract class for generic EntityResourceProvider instances
    /// </summary>
    internal interface IEntityResourceProviderInternal : IEntityResourceProvider
    {
        /// <summary>
        /// Should the given type be included in the claim of this entity resource provider?
        /// </summary>
        bool Include(Type type);

        /// <summary>
        /// Marks a collection of regular entity resource types as claimed by this entity resource provider
        /// </summary>
        void MakeClaimRegular(IEnumerable<Type> types);

        /// <summary>
        /// Marks a collection of wrapped entity resource types as claimed by this entity resource provider
        /// </summary>
        void MakeClaimWrapped(IEnumerable<Type> types);

        /// <summary>
        /// Triggers the collection of all procedural resources belonging to this entity resource provider
        /// </summary>
        void MakeClaimProcedural();

        /// <summary>
        /// Inserts the given resource as a new resource claimed by this entity resource provider
        /// </summary>
        void InsertProcedural(RESTableContext context, IProceduralEntityResource resource);

        /// <summary>
        /// Validates the entity resource provider
        /// </summary>
        void Validate();

        /// <summary>
        /// Returns all procedural entity resources from the provider. Used by RESTable internally. Don't call this method.
        /// </summary>
        IEnumerable<IProceduralEntityResource> SelectProceduralResources(RESTableContext context);

        /// <summary>
        /// Creates a new dynamic entity resource object with the given name, description and methods. Used by RESTable internally. Don't call this method.
        /// </summary>
        IProceduralEntityResource InsertProceduralResource(RESTableContext context, string name, string description, Method[] methods, dynamic data);

        /// <summary>
        /// Runs a given update operation. Used by RESTable internally. Don't call this method.
        /// </summary>
        void SetProceduralResourceMethods(RESTableContext context, IProceduralEntityResource resource, Method[] methods);

        /// <summary>
        /// Runs a given update operation. Used by RESTable internally. Don't call this method.
        /// </summary>
        void SetProceduralResourceDescription(RESTableContext context, IProceduralEntityResource resource, string newDescription);

        /// <summary>
        /// Deletes a dynamic entity resource entity. Used by RESTable internally. Don't call this method.
        /// </summary>
        bool DeleteProceduralResource(RESTableContext context, IProceduralEntityResource resource);

        /// <summary>
        /// The ReceiveClaimed method is called by RESTable once one or more resources provided
        /// by this ResourceProvider have been added. Override this to provide additional 
        /// logic once resources have been validated and set up.
        /// </summary>
        void ReceiveClaimed(ICollection<IEntityResource> claimedResources);

        /// <summary>
        /// An optional method for modifying the RESTable resource attribute of a type before the resource is generated
        /// </summary>
        void ModifyResourceAttribute(Type type, RESTableAttribute attribute);

        /// <summary>
        /// Removes the procedural resource belonging to the given type
        /// </summary>
        bool RemoveProceduralResource(Type type);

        TypeCache TypeCache { get; set; }
        ResourceValidator ResourceValidator { get; set; }
        ResourceCollection ResourceCollection { get; set; }
    }
}