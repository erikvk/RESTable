using System.Collections.Generic;
using System.Linq;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// A RESTable attribute type used for procedurally created resources
    /// </summary>
    internal class RESTableProceduralAttribute : RESTableAttribute
    {
        /// <inheritdoc />
        internal RESTableProceduralAttribute(IEnumerable<Method> methods) : base(methods.ToArray()) { }
    }
}