using System.Collections.Generic;

namespace RESTar
{
    public interface IOperationsProvider<T> :
        ISelector<T>,
        IInserter<T>,
        IUpdater<T>,
        IDeleter<T>
    {
    }

    public interface ISelector<out T>
    {
        IEnumerable<T> Select(IRequest request);
    }

    public interface IInserter<in T>
    {
        void Insert(IEnumerable<T> entities, IRequest request);
    }

    public interface IUpdater<in T>
    {
        void Update(IEnumerable<T> entities, IRequest request);
    }

    public interface IDeleter<in T>
    {
        void Delete(IEnumerable<T> entities, IRequest request);
    }
}