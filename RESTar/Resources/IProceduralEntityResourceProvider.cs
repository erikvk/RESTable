using System;
using System.Collections.Generic;

namespace RESTar.Resources {
    /// <summary>
    /// Describes the operations of a dynamic entity resource provider, that can create
    /// dynamic entity resources during runtime.
    /// </summary>
    public interface IProceduralEntityResourceProvider
    {
        /// <summary>
        /// The base namespace to place all resources in
        /// </summary>
        string BaseNamespace { get; }

        /// <summary>
        /// Returns the dynamic entity resource object with the given name
        /// </summary>
        IEnumerable<IProceduralEntityResource> Select();

        /// <summary>
        /// Creates a new dynamic entity resource object with the given name, description and methods
        /// </summary>
        IProceduralEntityResource Insert(string name, string description, Method[] methods, string alias);

        /// <summary>
        /// Runs a given update operation
        /// </summary>
        bool Update(IProceduralEntityResource resource, Func<bool> updater);

        /// <summary>
        /// Deletes a dynamic entity resource entity
        /// </summary>
        bool Delete(IProceduralEntityResource resource);
    }
}