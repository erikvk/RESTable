﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Internal.Transactions;
using static RESTar.RESTarConfig;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class AppRequest<T> : IRequest<T>, IDisposable where T : class
    {
        public Conditions Conditions { get; private set; }
        public IResource<T> Resource { get; }
        public bool Unsafe { get; set; }
        public Limit Limit { get; set; } = Limit.NoLimit;
        public OrderBy OrderBy { get; private set; }
        public string Body { get; set; }
        public string AuthToken { get; internal set; }
        public bool IsInternal => true;

        #region Explicit

        RESTarMethods IRequest.Method => default(RESTarMethods);
        IDictionary<string, string> IRequest.ResponseHeaders => null;
        IResource IRequest.Resource => Resource;

        MetaConditions IRequest.MetaConditions => new MetaConditions
        {
            OrderBy = OrderBy,
            Limit = Limit,
            Unsafe = Unsafe
        };

        #endregion

        public AppRequest()
        {
            if (!Initialized)
                throw new NotInitializedException();
            Resource = RESTar.Resource.Get<T>();
            if (Resource == null)
                throw new ArgumentException($"Unknown resource type '{typeof(T).FullName}'. " +
                                            "Must be a registered RESTar resource.");
        }

        internal void AddOrderBy((string key, bool descending)? orderBy)
        {
            if (orderBy == null) return;
            OrderBy = OrderBy ?? new OrderBy(Resource);
            OrderBy.SetStaticKey(orderBy.Value.key);
            OrderBy.Descending = orderBy.Value.descending;
        }

        internal void AddConditions(params (string key, Operator @operator, dynamic value)?[] conds)
        {
            if (conds == null || conds.All(c => c == null)) return;
            if (Conditions == null)
                Conditions = new Conditions(Resource);
            // ReSharper disable once PossibleInvalidOperationException
            conds.ForEach(c => Conditions.Add(c.Value.key, c.Value.@operator, c.Value.value));
        }

        private void Check(RESTarMethods method)
        {
            if (!Resource.AvailableMethods.Contains(method))
                throw new ForbiddenException(NotAuthorized,
                    $"{method} is not available for resource '{typeof(T).FullName}'");
        }

        private int INSERT(Func<IEnumerable<T>> inserter)
        {
            var count = 0;
            IEnumerable<T> results = null;
            try
            {
                #region Index

                if (Resource.TargetType == typeof(DatabaseIndex))
                {
                    results = inserter?.Invoke();
                    if (results == null) return 0;
                    return Resource.Insert(results, this);
                }

                #endregion

                Trans(() =>
                {
                    results = inserter?.Invoke();
                    if (results == null) return;
                    if (Resource.RequiresValidation)
                        results.Cast<IValidatable>().ForEach(r =>
                        {
                            if (!r.Validate(out string reason))
                                throw new ValidatableException(reason);
                        });
                    count = Resource.Insert(results, this);
                });
            }
            catch (Exception e)
            {
                if (results != null)
                    Trans(() =>
                    {
                        foreach (var item in results)
                            if (item != null)
                                Do.Try(() => item.Delete());
                    });
                throw new AbortedInserterException(e, this);
            }
            return count;
        }

        private int INSERT(Func<T> inserter)
        {
            var count = 0;
            T result = null;
            try
            {
                #region Index

                if (Resource.TargetType == typeof(DatabaseIndex))
                {
                    result = inserter?.Invoke();
                    if (result == null) return 0;
                    return Resource.Insert(new[] {result}, this);
                }

                #endregion

                Trans(() =>
                {
                    result = inserter?.Invoke();
                    if (result == null) return;
                    if (result is IValidatable v)
                        if (!v.Validate(out string reason))
                            throw new ValidatableException(reason);
                    count = Resource.Insert(new[] {result}, this);
                });
            }
            catch (Exception e)
            {
                if (result != null)
                    Trans(() => Do.Try(() => result.Delete()));
                throw new AbortedInserterException(e, this);
            }
            return count;
        }

        private int UPDATE(Func<IEnumerable<T>, IEnumerable<T>> updater,
            IEnumerable<T> source = null)
        {
            var count = 0;
            try
            {
                IEnumerable<T> results;
                source = source ?? Resource.Select(this);
                if (!Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(Resource);

                #region Index

                if (Resource.TargetType == typeof(DatabaseIndex))
                {
                    results = updater?.Invoke(source);
                    if (results == null) return 0;
                    return Resource.Update(results, this);
                }

                #endregion

                Trans(() =>
                {
                    results = updater?.Invoke(source);
                    if (results == null) return;
                    if (Resource.RequiresValidation)
                        results.Cast<IValidatable>().ForEach(r =>
                        {
                            if (!r.Validate(out string reason))
                                throw new ValidatableException(reason);
                        });
                    count = Resource.Update(results, this);
                });
                return count;
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e, this);
            }
        }

        private int UPDATE(Func<T, T> updater, T source = null)
        {
            var count = 0;
            try
            {
                T result;
                if (source == null)
                {
                    var matches = Resource.Select(this);
                    if (matches == null) return 0;
                    if (!Unsafe && matches.MoreThanOne())
                        throw new AmbiguousMatchException(Resource);
                    source = matches.First();
                }

                #region Index

                if (Resource.TargetType == typeof(DatabaseIndex))
                {
                    result = updater?.Invoke(source);
                    if (result == null) return 0;
                    return Resource.Update(new[] {result}, this);
                }

                #endregion

                Trans(() =>
                {
                    result = updater?.Invoke(source);
                    if (result == null) return;
                    if (result is IValidatable v)
                        if (!v.Validate(out string reason))
                            throw new ValidatableException(reason);
                    count = Resource.Update(new[] {result}, this);
                });
                return count;
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e, this);
            }
        }

        public IEnumerable<T> GET()
        {
            Check(RESTarMethods.GET);
            try
            {
                return StaticSELECT(this);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, this);
            }
        }

        internal static IEnumerable<T> StaticSELECT(IRequest<T> request)
        {
            request.MetaConditions.Unsafe = true;
            var results = request.Resource.Select(request);
            if (results == null) return null;
            if (request.MetaConditions.OrderBy != null)
                results = results.Filter(request.MetaConditions.OrderBy);
            if (request.MetaConditions.Limit != -1)
                results = results.Filter(request.MetaConditions.Limit);
            return results;
        }

        public int POST(Func<T> inserter)
        {
            Check(RESTarMethods.POST);
            return INSERT(inserter);
        }

        public int POST(Func<IEnumerable<T>> inserter)
        {
            Check(RESTarMethods.POST);
            return INSERT(inserter);
        }

        public int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater)
        {
            Check(RESTarMethods.PATCH);
            return UPDATE(updater);
        }

        public int PUT(Func<T> inserter, Func<T, T> updater)
        {
            Check(RESTarMethods.PUT);
            try
            {
                Unsafe = false;
                var source = Resource.Select(this);
                switch (source?.Count())
                {
                    case null: return 0;
                    case 0: return INSERT(inserter);
                    case 1: return UPDATE(updater, source.First());
                    default: throw new AmbiguousMatchException(Resource);
                }
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, this);
            }
        }

        public int DELETE()
        {
            Check(RESTarMethods.DELETE);
            try
            {
                var count = 0;
                var source = Resource.Select(this);
                if (source == null) return 0;
                if (!Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(Resource);

                #region Index

                if (typeof(T) == typeof(DatabaseIndex))
                    return Resource.Delete(source, this);

                #endregion

                Trans(() => count = Resource.Delete(source, this));
                return count;
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e, this);
            }
        }

        public void Dispose()
        {
            if (IsInternal) return;
            AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}