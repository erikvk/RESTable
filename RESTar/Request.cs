using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RESTar.Requests;

// ReSharper disable RedundantUsingDirective
// ReSharper disable UnusedMember.Global
using RESTar.Deflection;
using RESTar.Internal;
using Starcounter;
using static RESTar.Deflection.TypeCache;
using static RESTar.Internal.RESTarResourceType;

namespace RESTar
{
    internal static class SQLCache
    {
        internal static readonly IDictionary<int, string> SQLQueries;
        static SQLCache() => SQLQueries = new ConcurrentDictionary<int, string>();
    }

    /// <summary>
    /// Used to create internal RESTar requests
    /// </summary>
    /// <typeparam name="T">The resource type to query</typeparam>
    public static class Request<T> where T : class
    {
        #region GET

        private static readonly string _SELECT = $"SELECT t FROM {typeof(T).FullName} t";

        /// <summary>
        /// Returns all entitites in the resource that matches a condition.
        /// </summary>
        /// <param name="key">The condition key</param>
        /// <param name="operator">The condition operator</param>
        /// <param name="value">The conditions value</param>
        public static IEnumerable<T> GET(string key, Operator @operator, dynamic value)
        {
            if (!RESTarConfig.ResourceByType.TryGetValue(typeof(T), out Internal.IResource resource))
                throw new ArgumentException($"Unknown resource '{typeof(T).FullName}'. Not a RESTar resource.");

            switch (resource.ResourceType)
            {
                case ScStatic:
                    var th = typeof(T).GetHashCode();
                    var kh = key.GetHashCode();
                    var oh = @operator.GetHashCode();
                    var ah = resource.DynamicConditionsAllowed.GetHashCode();
                    var ph = th + kh + ah;
                    var propChain = PropertyChain.GetOrMake(ph, resource, key, resource.DynamicConditionsAllowed);
                    if (propChain.ScQueryable)
                    {
                        var sh = th + kh + oh;
                        if (!SQLCache.SQLQueries.TryGetValue(sh, out string query))
                            query = SQLCache.SQLQueries[sh] = $"{_SELECT} WHERE t.{key.Fnuttify()} =?";
                        return Db.SQL<T>(query, value);
                    }
                    throw new ArgumentOutOfRangeException();
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="conditions">A list of conditions to match against</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        public static IEnumerable<T> GET((string key, Operator @operator, dynamic value)?[] conditions,
            int limit, (string key, bool descending)? orderBy)
        {
            var ar = new AppRequest<T> {Unsafe = true, Limit = limit};
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.GET();
        }

        static decimal Time(Action action)
        {
            var s = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 1; i < 300000; i++)
                action();
            s.Stop();
            return s.ElapsedMilliseconds;
        }

        /// <summary>
        /// Returns all entitites in the resource that matches a condition. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="condition">A condition to match against</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition = null,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9
            }, limit, orderBy);

        /// <summary>
        /// Returns all entitites in the resource that matches a set of conditions. To order
        /// the entitites, include an orderBy tuple. To restrict the entities to a certain cardinality,
        /// include a limit.
        /// </summary>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <param name="condition10">A condition to match against</param>
        public static IEnumerable<T> GET(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            GET(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9, condition10
            }, limit, orderBy);

        #endregion

        #region POST

        /// <summary>
        /// Inserts an entity into the specified resource. The inserter delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <returns>The number of entities successfully inserted (0 or 1)</returns>
        public static int POST(Func<T> inserter) => new AppRequest<T>().POST(inserter);

        /// <summary>
        /// Inserts one or more entities into the specified resource. The inserter delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns an IEnumerable of entities of the resource type</param>
        /// <returns>The number of entities successfully inserted</returns>
        public static int POST(Func<IEnumerable<T>> inserter) => new AppRequest<T>().POST(inserter);

        #endregion

        #region PATCH

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="conditions">A list of conditions to match against</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)?[] conditions,
            bool @unsafe, int limit, (string key, bool descending)? orderBy = null)
        {
            var ar = new AppRequest<T> {Unsafe = @unsafe, Limit = limit};
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.PATCH(updater);
        }

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition = null,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Updates one more entities in the specified resource. The updater delegate will automatically
        /// be invoked within a transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="updater">A function that is applied to the matched entities and returns an IEnumerable of
        /// updated entities</param>
        /// <param name="unsafe">If true, the request will update multiple entities if more than one is matched by
        /// the conditions and limit. Else RESTar will return an error in those cases.</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <param name="condition10">A condition to match against</param>
        /// <returns>The number of entities successfully updated</returns>
        public static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            PATCH(updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9, condition10
            }, @unsafe, limit, orderBy);

        #endregion

        #region PUT

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="conditions">A list of conditions to match against</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)?[] conditions,
            int limit = -1, (string key, bool descending)? orderBy = null)
        {
            var ar = new AppRequest<T> {Limit = limit};
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.PUT(inserter, updater);
        }

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition = null,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9
            }, limit, orderBy);

        /// <summary>
        /// Matches against existing entities and tries to locate a single uniquely matched
        /// entity. If such an entity is found, the provided updater is invoked with it as
        /// argument. Else the inserter is invoked. If no unique match can be made, an exception
        /// will be thrown. The delegate invocation will automatically be performed within a 
        /// transaction scope if the resource is a Starcounter database type.
        /// </summary>
        /// <param name="inserter">A function that returns one entity of the resource type.</param>
        /// <param name="updater">A function that is applied to the matched entity and returns an 
        /// updated entity</param>
        /// <param name="limit">The number of entities to restrict the matching to</param>
        /// <param name="orderBy">To order the matched entities, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <param name="condition10">A condition to match against</param>
        /// <returns>The number of entities successfully updated or inserted (0 or 1)</returns>
        public static int PUT(Func<T> inserter, Func<T, T> updater,
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            int limit = -1, (string key, bool descending)? orderBy = null) =>
            PUT(inserter, updater, new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9, condition10
            }, limit, orderBy);

        #endregion

        #region DELETE

        /// <summary>
        /// Deletes an entitity in the resource uniquely individuated by a condition, or throws 
        /// an exception if a single entity cannot be located.
        /// </summary>
        /// <param name="key">The condition key</param>
        /// <param name="operator">The condition operator</param>
        /// <param name="value">The conditions value</param>
        public static int DELETE(string key, Operator @operator, dynamic value) => DELETE((key, @operator, value));

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="conditions">A list of conditions to match against</param>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE((string key, Operator @operator, dynamic value)?[] conditions,
            bool @unsafe, int limit, (string key, bool descending)? orderBy)
        {
            var ar = new AppRequest<T> {Unsafe = @unsafe, Limit = limit};
            ar.AddOrderBy(orderBy);
            ar.AddConditions(conditions);
            return ar.DELETE();
        }

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition = null,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9
            }, @unsafe, limit, orderBy);

        /// <summary>
        /// Deletes one or more entitites from the specified resource.
        /// </summary>
        /// <param name="unsafe">If true, RESTar will delete all entities matched after applying the given
        /// conditions and limit. If false, RESTar will only delete a uniquely matched entity, and throw 
        /// an exception if more than one entity was matched</param>
        /// <param name="limit">The number of entities to restrict the response to</param>
        /// <param name="orderBy">To order the response, include the property name to order by as key and 
        /// whether the ordering should be in descending order (as opposed to ascending)</param>
        /// <param name="condition1">A condition to match against</param>
        /// <param name="condition2">A condition to match against</param>
        /// <param name="condition3">A condition to match against</param>
        /// <param name="condition4">A condition to match against</param>
        /// <param name="condition5">A condition to match against</param>
        /// <param name="condition6">A condition to match against</param>
        /// <param name="condition7">A condition to match against</param>
        /// <param name="condition8">A condition to match against</param>
        /// <param name="condition9">A condition to match against</param>
        /// <param name="condition10">A condition to match against</param>
        /// <returns>The number of entities successfully deleted</returns>
        public static int DELETE(
            (string key, Operator @operator, dynamic value)? condition1,
            (string key, Operator @operator, dynamic value)? condition2,
            (string key, Operator @operator, dynamic value)? condition3,
            (string key, Operator @operator, dynamic value)? condition4,
            (string key, Operator @operator, dynamic value)? condition5,
            (string key, Operator @operator, dynamic value)? condition6,
            (string key, Operator @operator, dynamic value)? condition7,
            (string key, Operator @operator, dynamic value)? condition8,
            (string key, Operator @operator, dynamic value)? condition9,
            (string key, Operator @operator, dynamic value)? condition10,
            bool @unsafe = false, int limit = -1, (string key, bool descending)? orderBy = null) =>
            DELETE(new[]
            {
                condition1, condition2, condition3, condition4, condition5,
                condition6, condition7, condition8, condition9, condition10
            }, @unsafe, limit, orderBy);

        #endregion
    }
}