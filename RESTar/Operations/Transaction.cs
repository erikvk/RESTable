//using System;
//using RESTar.Admin;
//using Starcounter;
//
//namespace RESTar.Operations
//{
///// <inheritdoc />
//internal class Transaction<T> : Transaction where T : class
//{
//    /// <summary>
//    /// Performs the action delegate synchronously inside a transaction scope, if
//    /// the resource type requires it
//    /// </summary>
//    public new TResult LrScope<TResult>(Func<TResult> action)
//    {
//        if (action == null)
//            throw new ArgumentNullException(nameof(action));
//        if (typeof(T) == typeof(DatabaseIndex))
//            return action();
//        return base.Scope(action);
//    }

//    public static TResult LrTransact<TResult>(Func<TResult> action)
//    {
//        if (typeof(T) == typeof(DatabaseIndex))
//            return action();
//        var t = new Transaction();
//        try
//        {
//            var results = t.Scope(action);
//            t.Commit();
//            return results;
//        }
//        catch
//        {
//            t.Rollback();
//            throw;
//        }
//    }

//    public static void LrTransact(Action action)
//    {
//        if (typeof(T) == typeof(DatabaseIndex))
//            action();
//        else
//        {
//            var t = new Transaction();
//            try
//            {
//                t.Scope(action);
//                t.Commit();
//            }
//            catch
//            {
//                t.Rollback();
//                throw;
//            }
//        }
//    }

//    public static TResult ShTransact<TResult>(Func<TResult> action)
//    {
//        if (typeof(T) == typeof(DatabaseIndex))
//            return action();
//        var results = default(TResult);
//        Db.TransactAsync(() => results = action());
//        return results;
//    }

//    public static void ShTransact(Action action)
//    {
//        if (typeof(T) == typeof(DatabaseIndex))
//            action();
//        else Db.TransactAsync(action);
//    }
//}
//}