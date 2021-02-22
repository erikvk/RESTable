using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Delete operation used in DELETE. Takes a set of entities and deletes them from 
    /// the resource, and returns the number of entities successfully deleted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate ValueTask<int> AsyncDeleter<T>(IRequest<T> request) where T : class;
}