using System.Collections.Generic;

namespace RESTar.Operations
{
    /// <summary>
    /// Specifies the Select operation used in GET, PATCH, PUT and DELETE. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate IEnumerable<T> Selector<out T>(IRequest request);

    /// <summary>
    /// Specifies the Insert operation used in POST and PUT. Takes a set of entities and inserts 
    /// them into the resource, and returns the number of entities successfully inserted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Inserter<in T>(IEnumerable<T> entities, IRequest request);

    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// their corresponding entities in the resource (often by deleting the old ones and inserting 
    /// the new), and returns the number of entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Updater<in T>(IEnumerable<T> entities, IRequest request);

    /// <summary>
    /// Specifies the Delete operation used in DELETE. Takes a set of entities and deletes them from 
    /// the resource, and returns the number of entities successfully deleted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Deleter<in T>(IEnumerable<T> entities, IRequest request);
}