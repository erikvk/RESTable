using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Deflection;
using IResource = RESTar.Internal.IResource;

#pragma warning disable 1591

namespace RESTar
{
    public class Request<T> : IRequest<T>, IDisposable where T : class
    {
        public IResource<T> Resource { get; }
        public Conditions Conditions { get; }
        public string Body { get; set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        public MetaConditions MetaConditions { get; }
        public RESTarMethods Method { get; private set; }

        internal int SqlHash { get; private set; }
        internal string SqlQuery { get; private set; }
        internal object[] SqlValues { get; private set; }

        internal bool HasChanged => Conditions.HasChanged;

        internal void Prep()
        {
            SqlHash = Conditions.Prep();
            if (!Conditions.SQL.Any())
                SqlQuery = StarcounterOperations<T>.SELECT;
            else
            {
                var wh = Conditions.SQL.MakeWhereClause();
                SqlQuery = $"{StarcounterOperations<T>.SELECT}{wh?.WhereString}";
                SqlValues = Conditions.SQL.Select(c => c.Value).ToArray();
            }
        }

        public Request(string key, Operator op, object value) : this((key, op, value))
        {
        }

        public Request(params (string key, Operator op, object value)[] conditions)
        {
            Resource = RESTar.Resource.Get<T>() ?? throw new ArgumentException($"'{typeof(T).FullName}' " +
                                                                               "is not a RESTar resource.");
            ResponseHeaders = new Dictionary<string, string>();
            Conditions = new Conditions(Resource);
            MetaConditions = new MetaConditions {Unsafe = true};
            conditions?.Select(c => new Condition(
                propertyChain: PropertyChain.GetOrMake(Resource, c.key, Resource.DynamicConditionsAllowed),
                op: c.op,
                value: c.value
            )).ForEach(Conditions.Add);
        }

        private void Check(RESTarMethods method)
        {
            if (!Resource.AvailableMethods.Contains(method))
                throw new ForbiddenException(ErrorCodes.NotAuthorized,
                    $"{method} is not available for resource '{typeof(T).FullName}'");
        }

        public IEnumerable<T> GET()
        {
            if (HasChanged) Prep();
            Method = RESTarMethods.GET;
            Check(Method);
            using (Auth) return Evaluators<T>.AppSELECT(this);
        }

        public int COUNT()
        {
            if (HasChanged) Prep();
            Method = RESTarMethods.GET;
            Check(Method);
            using (Auth) return Evaluators<T>.AppSELECT(this).Count();
        }

        public int POST(Func<T> inserter)
        {
            Method = RESTarMethods.POST;
            Check(Method);
            using (Auth) return Evaluators<T>.App.POST(inserter, this);
        }

        public int POST(Func<IEnumerable<T>> inserter)
        {
            Method = RESTarMethods.POST;
            Check(Method);
            using (Auth) return Evaluators<T>.App.POST(inserter, this);
        }

        public int PATCH(Func<T, T> updater)
        {
            if (HasChanged) Prep();
            Method = RESTarMethods.PATCH;
            Check(Method);
            using (Auth)
            {
                var source = Evaluators<T>.AppSELECT(this);
                if (source.IsNullOrEmpty()) return 0;
                if (source.MoreThanOne())
                    throw new AmbiguousMatchException(Resource);
                return Evaluators<T>.App.PATCH(updater, source.First(), this);
            }
        }

        public int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater)
        {
            if (HasChanged) Prep();
            Method = RESTarMethods.PATCH;
            Check(Method);
            using (Auth)
            {
                var source = Evaluators<T>.AppSELECT(this);
                if (source.IsNullOrEmpty()) return 0;
                return Evaluators<T>.App.PATCH(updater, source, this);
            }
        }

        public int PUT(Func<T> inserter, Func<T, T> updater)
        {
            if (HasChanged) Prep();
            Method = RESTarMethods.PUT;
            Check(Method);
            using (Auth)
            {
                var source = Evaluators<T>.AppSELECT(this);
                if (source == null) return 0;
                return Evaluators<T>.App.PUT(inserter, updater, source, this);
            }
        }

        public int DELETE(bool @unsafe = false)
        {
            if (HasChanged) Prep();
            Method = RESTarMethods.DELETE;
            Check(Method);
            using (Auth)
            {
                var source = Evaluators<T>.AppSELECT(this);
                if (source.IsNullOrEmpty()) return 0;
                if (@unsafe)
                    return Evaluators<T>.App.DELETE(source, this);
                if (source.MoreThanOne())
                    throw new AmbiguousMatchException(Resource);
                return Evaluators<T>.App.DELETE(source.First(), this);
            }
        }

        private Request<T> Auth
        {
            get
            {
                this.Authenticate();
                return this;
            }
        }

        public void Dispose()
        {
            Method = default(RESTarMethods);
            RESTarConfig.AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}