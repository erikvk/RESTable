using System;

namespace RESTar.Operations
{
    internal static class Do
    {
        internal static T Try<T>(Func<T> thingy, T onfail)
        {
            try
            {
                return thingy();
            }
            catch
            {
                return onfail;
            }
        }

        internal static T TryAndThrow<T>(Func<T> thingy, Exception onFail)
        {
            try
            {
                return thingy();
            }
            catch
            {
                throw onFail;
            }
        }

        internal static void TryCatch(Action thingy, Action<Exception> onCatch)
        {
            try
            {
                thingy();
            }
            catch (Exception e)
            {
                onCatch(e);
            }
        }

        internal static T TryAndThrow<T>(Func<T> thingy, string onFailMessage)
        {
            try
            {
                return thingy();
            }
            catch
            {
                throw new Exception(onFailMessage);
            }
        }

        internal static void Try(Action thingy)
        {
            try
            {
                thingy();
            }
            catch
            {
            }
        }

        internal static T Run<T>(Action thingy, T @return = default(T))
        {
            thingy();
            return @return;
        }

        internal static T Run<T>(Func<T> thingy)
        {
            return thingy();
        }
    }
}