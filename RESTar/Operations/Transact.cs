using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Db.TransactAsync(() => result = action());
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
            Db.TransactAsync(action);
        }

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope. 
        /// Uses Db.TransactAsync
        /// </summary>
        public static void TransAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Scheduling.ScheduleTask(() => Db.TransactAsync(action));
        }

        #endregion
    }
}
