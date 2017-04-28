using System.Collections.Generic;

namespace RESTar.Operations
{
    public delegate IEnumerable<T> Selector<out T>(IRequest request);
    public delegate int Inserter<in T>(IEnumerable<T> entities, IRequest request);
    public delegate int Updater<in T>(IEnumerable<T> entities, IRequest request);
    public delegate int Deleter<in T>(IEnumerable<T> entities, IRequest request);
}