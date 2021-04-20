using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// the new), and returns the number of entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate int Updater<T>(IRequest<T> request) where T : class;
}