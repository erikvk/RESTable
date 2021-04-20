using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// their corresponding entities in the resource (often by deleting the old ones and inserting 
    /// the new), and returns the number of entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate ValueTask<int> AsyncUpdater<T>(IRequest<T> request) where T : class;
}