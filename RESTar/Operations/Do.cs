﻿using System;

#pragma warning disable 1591

namespace RESTar.Operations
{
    public static class Do
    {
        public static T Try<T>(Func<T> thingy, T onfail)
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

        public static T Try<T>(Action thingy, T onfail)
        {
            try
            {
                thingy();
                return default(T);
            }
            catch
            {
                return onfail;
            }
        }

        public static T Try<T>(Func<T> thingy, Func<T> onFail)
        {
            try
            {
                return thingy();
            }
            catch
            {
                return onFail();
            }
        }

        public static T TryAndThrow<T>(Func<T> thingy, Exception onFail)
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

        public static void TryCatch(Action thingy, Action<Exception> onCatch)
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

        public static T TryAndThrow<T>(Func<T> thingy, string onFailMessage)
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

        public static void Try(Action thingy)
        {
            try
            {
                thingy();
            }
            catch
            {
            }
        }

        public static T Run<T>(Action thingy, T @return = default(T))
        {
            thingy();
            return @return;
        }

        public static T Run<T>(Func<T> thingy)
        {
            return thingy();
        }
    }
}