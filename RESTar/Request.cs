using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using static RESTar.Internal.RESTarResourceType;
using IResource = RESTar.Internal.IResource;

#pragma warning disable 1591

namespace RESTar
{
    public class Request<T> : IRequest<T> where T : class
    {
        public IResource<T> Resource { get; }
        public Conditions<T> Conditions { get; }
        public string Body { get; set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        public MetaConditions MetaConditions { get; }
        RESTarMethods IRequest.Method => 0;

        private bool ScSql;
        internal string SqlQuery { get; private set; }
        internal object[] SqlValues { get; private set; }

        private bool GETAllowed;
        private bool POSTAllowed;
        private bool PATCHAllowed;
        private bool PUTAllowed;
        private bool DELETEAllowed;

        internal void Prep()
        {
            if (!Conditions.SQL.Any())
                SqlQuery = StarcounterOperations<T>.SELECT;
            else
            {
                var wh = Conditions.SQL.MakeWhereClause();
                SqlQuery = $"{StarcounterOperations<T>.SELECT}{wh.WhereString}";
                SqlValues = wh.Values;
            }
            Conditions.ResetStatus();
        }

        public Request(string key, Operator op, object value) : this((key, op, value))
        {
        }

        public Request(params (string key, Operator op, object value)[] conditions)
        {
            Resource = Resource<T>.Get;
            ResponseHeaders = new Dictionary<string, string>();
            Conditions = new Conditions<T>();
            MetaConditions = new MetaConditions {Unsafe = true};
            conditions?.Select(c => new Condition<T>(
                propertyChain: Resource.MakePropertyChain(c.key, Resource.DynamicConditionsAllowed),
                op: c.op,
                value: c.value
            )).ForEach(Conditions.Add);
            this.Authenticate();
            ScSql = Resource.ResourceType == StaticStarcounter;
            Resource.AvailableMethods.ForEach(m =>
            {
                switch (m)
                {
                    case RESTarMethods.GET:
                        GETAllowed = true;
                        break;
                    case RESTarMethods.POST:
                        POSTAllowed = true;
                        break;
                    case RESTarMethods.PATCH:
                        PATCHAllowed = true;
                        break;
                    case RESTarMethods.PUT:
                        PUTAllowed = true;
                        break;
                    case RESTarMethods.DELETE:
                        DELETEAllowed = true;
                        break;
                }
            });
            if (ScSql) Prep();
        }

        private static Exception Deny(RESTarMethods method) => new ForbiddenException
            (ErrorCodes.NotAuthorized, $"{method} is not available for resource '{typeof(T).FullName}'");

        public IEnumerable<T> GET()
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!GETAllowed) throw Deny(RESTarMethods.GET);
            return Evaluators<T>.RAW_SELECT(this);
        }

        public bool ANY()
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!GETAllowed) throw Deny(RESTarMethods.GET);
            return Evaluators<T>.RAW_SELECT(this).Any();
        }

        public int COUNT()
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!GETAllowed) throw Deny(RESTarMethods.GET);
            return Evaluators<T>.RAW_SELECT(this).Count();
        }

        public int POST(Func<T> inserter)
        {
            if (!POSTAllowed) throw Deny(RESTarMethods.POST);
            return Evaluators<T>.App.POST(inserter, this);
        }

        public int POST(Func<IEnumerable<T>> inserter)
        {
            if (!POSTAllowed) throw Deny(RESTarMethods.POST);
            return Evaluators<T>.App.POST(inserter, this);
        }

        public int PATCH(Func<T, T> updater)
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!PATCHAllowed) throw Deny(RESTarMethods.PATCH);
            var source = Evaluators<T>.RAW_SELECT(this);
            if (source.IsNullOrEmpty()) return 0;
            if (source.MoreThanOne())
                throw new AmbiguousMatchException(Resource);
            return Evaluators<T>.App.PATCH(updater, source.First(), this);
        }

        public int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater)
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!PATCHAllowed) throw Deny(RESTarMethods.PATCH);
            var source = Evaluators<T>.RAW_SELECT(this);
            if (source.IsNullOrEmpty()) return 0;
            return Evaluators<T>.App.PATCH(updater, source, this);
        }

        public int PUT(Func<T> inserter, Func<T, T> updater)
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!PUTAllowed) throw Deny(RESTarMethods.PUT);
            var source = Evaluators<T>.RAW_SELECT(this);
            if (source == null) return 0;
            return Evaluators<T>.App.PUT(inserter, updater, source, this);
        }

        public int DELETE(bool @unsafe = false)
        {
            if (ScSql && Conditions.HasChanged) Prep();
            if (!DELETEAllowed) throw Deny(RESTarMethods.DELETE);
            var source = Evaluators<T>.RAW_SELECT(this);
            if (source.IsNullOrEmpty()) return 0;
            if (@unsafe)
                return Evaluators<T>.App.DELETE(source, this);
            if (source.MoreThanOne())
                throw new AmbiguousMatchException(Resource);
            return Evaluators<T>.App.DELETE(source.First(), this);
        }
    }
}