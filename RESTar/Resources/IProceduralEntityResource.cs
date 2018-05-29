using System;

namespace RESTar.Resources {
    /// <summary>
    /// Describes a dynamic entity resource
    /// </summary>
    public interface IProceduralEntityResource
    {
        /// <summary>
        /// The name, including namespace, of this resource
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The description to use for the resource
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// The methods to enable for the resource
        /// </summary>
        Method[] Methods { get; set; }

        /// <summary>
        /// The type to bind to this resource. Must be unique for this resource.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Is this resource editable?
        /// </summary>
        bool Editable { get; }
    }
}