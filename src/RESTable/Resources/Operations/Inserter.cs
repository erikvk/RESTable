using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Insert operation used in POST and PUT. Takes a set of entities and inserts 
    /// them into the resource, and returns the number of entities successfully inserted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate int Inserter<T>(IRequest<T> request) where T : class;
}