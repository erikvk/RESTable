using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Select operation used in GET, PATCH, PUT and DELETE. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate Task<IEnumerable<T>> AsyncSelector<T>(IRequest<T> request) where T : class;
}