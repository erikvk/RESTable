using System.Collections.Generic;

namespace RESTar
{
    public interface ISelector<out T>
    {
        IEnumerable<T> Select(IRequest request);
    }

    public interface IInserter<in T>
    {
        int Insert(IEnumerable<T> entities, IRequest request);
    }

    public interface IUpdater<in T>
    {
        int Update(IEnumerable<T> entities, IRequest request);
    }

    public interface IDeleter<in T>
    {
        int Delete(IEnumerable<T> entities, IRequest request);
    }

    internal interface IOperationsProvider<T> :
        ISelector<T>,
        IInserter<T>,
        IUpdater<T>,
        IDeleter<T>
    {
    }
}