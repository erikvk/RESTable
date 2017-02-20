using System;
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

    public class OperationsProvider<T> : IOperationsProvider<T>
    {
        internal Selector<T> Selector;
        internal Inserter<T> Inserter;
        internal Updater<T> Updater;
        internal Deleter<T> Deleter;

        public IEnumerable<T> Select(IRequest request) => Selector.Select(request);
        public int Insert(IEnumerable<T> entities, IRequest request) => Inserter.Insert(entities, request);
        public int Update(IEnumerable<T> entities, IRequest request) => Updater.Update(entities, request);
        public int Delete(IEnumerable<T> entities, IRequest request) => Deleter.Delete(entities, request);

        public bool Supports(IEnumerable<RESTarOperations> operations)
        {
            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case RESTarOperations.Select:
                        if (Selector == null) return false;
                        break;
                    case RESTarOperations.Insert:
                        if (Inserter == null) return false;
                        break;
                    case RESTarOperations.Update:
                        if (Updater == null) return false;
                        break;
                    case RESTarOperations.Delete:
                        if (Deleter == null) return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return true;
        }
    }

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

    public class Selector<T> : ISelector<T>
    {
        private readonly Func<IRequest, IEnumerable<T>> _select;

        public Selector(Func<IRequest, IEnumerable<T>> select)
        {
            if (select == null) throw new ArgumentNullException(nameof(select));
            _select = select;
        }

        public IEnumerable<T> Select(IRequest request) => _select(request);
    }

    public class Inserter<T> : IInserter<T>
    {
        private readonly Func<IEnumerable<T>, IRequest, int> _insert;

        public Inserter(Func<IEnumerable<T>, IRequest, int> insert)
        {
            if (insert == null) throw new ArgumentNullException(nameof(insert));
            _insert = insert;
        }

        public int Insert(IEnumerable<T> entities, IRequest request) => _insert(entities, request);
    }

    public class Updater<T> : IUpdater<T>
    {
        private readonly Func<IEnumerable<T>, IRequest, int> _update;

        public Updater(Func<IEnumerable<T>, IRequest, int> update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));
            _update = update;
        }

        public int Update(IEnumerable<T> entities, IRequest request) => _update(entities, request);
    }

    public class Deleter<T> : IDeleter<T>
    {
        private readonly Func<IEnumerable<T>, IRequest, int> _delete;

        public Deleter(Func<IEnumerable<T>, IRequest, int> delete)
        {
            if (delete == null) throw new ArgumentNullException(nameof(delete));
            _delete = delete;
        }

        public int Delete(IEnumerable<T> entities, IRequest request) => _delete(entities, request);
    }
}