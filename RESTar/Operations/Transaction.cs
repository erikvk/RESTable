using System;
using Starcounter;

namespace RESTar.Operations
{
    /// <summary>
    /// Long running transaction
    /// </summary>
    internal class Transaction<T> : Transaction where T : class
    {
        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope, if
        /// the resource type requires it
        /// </summary>
        public new TResult Scope<TResult>(Func<TResult> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (typeof(T) == typeof(DatabaseIndex))
                return action();
            return base.Scope(action);
        }

        public static TResult Transact<TResult>(Func<TResult> action)
        {
            if (typeof(T) == typeof(DatabaseIndex))
                return action();
            var t = new Transaction();
            try
            {
                var results = t.Scope(action);
                t.Commit();
                return results;
            }
            catch
            {
                t.Rollback();
                throw;
            }
        }

        public static TResult TransactShort<TResult>(Func<TResult> action)
        {
            if (typeof(T) == typeof(DatabaseIndex))
                return action();
            var results = default(TResult);
            Db.TransactAsync(() => results = action());
            return results;
        }
    }
}