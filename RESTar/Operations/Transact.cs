using System;
using Starcounter;

namespace RESTar.Operations
{
    internal static class Transact
    {
        #region Simple

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope and
        /// returns the result of the transaction. Uses Db.TransactAsync
        /// </summary>
        public static T Trans<T>(Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var result = default(T);
            try
            {
                Db.TransactAsync(() => result = action());
            }
            catch (DbException e)
            {
                Log.Error($"!!! Transaction error at {e.StackTrace}");
            }
            return result;
        }

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope. 
        /// Uses Db.TransactAsync
        /// </summary>
        public static void Trans(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            try
            {
                Db.TransactAsync(action);
            }
            catch (DbException e)
            {
                Log.Error($"!!! Transaction error at {e.StackTrace}");
            }
        }

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope. 
        /// Uses Db.TransactAsync
        /// </summary>
        public static void TransAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Scheduling.RunTask(() =>
            {
                try
                {
                    Db.TransactAsync(action);
                }
                catch (DbException e)
                {
                    Log.Error($"!!! Transaction error at {e.StackTrace}");
                }
            });
        }

        #endregion
    }
}