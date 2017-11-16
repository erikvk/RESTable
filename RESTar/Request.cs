using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Serialization;
using static RESTar.Internal.ErrorCodes;
using IResource = RESTar.Internal.IResource;

#pragma warning disable 1591

namespace RESTar
{
    public class Request<T> : IRequest<T> where T : class
    {
        public IResource<T> Resource { get; }
        private Condition<T>[] _conditions;

        public Condition<T>[] Conditions
        {
            get => _conditions;
            set
            {
                _conditions = value ?? new Condition<T>[0];
                if (ScSql) BuildSQL();
            }
        }

        public Stream Body { get; set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        public MetaConditions MetaConditions { get; }
        public Origin Origin { get; }
        Methods IRequest.Method => 0;
        public ITarget<T> Target { get; set; }

        private readonly bool ScSql;
        internal string SelectQuery { get; private set; }
        internal string CountQuery { get; private set; }
        internal object[] SqlValues { get; private set; }
        private Dictionary<int, int> ValuesAssignments;

        private bool GETAllowed;
        private bool POSTAllowed;
        private bool PATCHAllowed;
        private bool PUTAllowed;
        private bool DELETEAllowed;

        internal void Prep()
        {
            if (!ScSql) return;
            var valueChanged = false;
            foreach (var cond in Conditions.Where(c => !c.Skip))
            {
                if (cond.HasChanged)
                {
                    BuildSQL();
                    Conditions.ResetStatus();
                    return;
                }
                if (cond.ValueChanged) valueChanged = true;
            }
            if (!valueChanged) return;
            Conditions.Where(c => !c.Skip).ForEach((cond, cindex) => SqlValues[ValuesAssignments[cindex]] = cond.Value);
            Conditions.ResetStatus();
        }

        private void BuildSQL()
        {
            if (!Conditions.HasSQL(out var sql))
            {
                SelectQuery = StarcounterOperations<T>.SELECT;
                CountQuery = StarcounterOperations<T>.COUNT;
                SqlValues = null;
                ValuesAssignments = null;
            }
            else
            {
                var wh = sql.MakeWhereClause(out var assignments);
                SelectQuery = $"{StarcounterOperations<T>.SELECT}{wh.WhereString}";
                CountQuery = $"{StarcounterOperations<T>.COUNT}{wh.WhereString}";
                SqlValues = wh.Values;
                ValuesAssignments = assignments;
            }
        }

        public Request(params Condition<T>[] conditions)
        {
            if (!RESTarConfig.Initialized)
                throw new NotInitializedException();
            Resource = Resource<T>.Get;
            Target = Resource;
            ResponseHeaders = new Dictionary<string, string>();
            MetaConditions = new MetaConditions {Unsafe = true};
            Origin = Origin.Internal;
            Conditions = conditions;
            this.Authenticate();
            ScSql = Resource.Provider == typeof(StarcounterProvider).GetProviderId();
            Resource.AvailableMethods.ForEach(m =>
            {
                switch (m)
                {
                    case Methods.GET:
                        GETAllowed = true;
                        break;
                    case Methods.POST:
                        POSTAllowed = true;
                        break;
                    case Methods.PATCH:
                        PATCHAllowed = true;
                        break;
                    case Methods.PUT:
                        PUTAllowed = true;
                        break;
                    case Methods.DELETE:
                        DELETEAllowed = true;
                        break;
                }
            });
            if (ScSql) BuildSQL();
        }

        public Request<T> WithConditions(IEnumerable<Condition<T>> conditions)
        {
            if (conditions is Condition<T>[] arr)
                Conditions = arr;
            else Conditions = conditions?.ToArray();
            return this;
        }

        public Request<T> WithConditions(params Condition<T>[] conditions)
        {
            Conditions = conditions;
            return this;
        }

        public Request<T> WithConditions(params (string key, Operator op, object value)[] conditions)
        {
            Conditions = conditions?.Any() != true
                ? new Condition<T>[0]
                : conditions.Select(c => new Condition<T>(
                    term: Resource.MakeConditionTerm(c.key),
                    op: c.op,
                    value: c.value
                )).ToArray();
            return this;
        }

        public Request<T> WithConditions(string key, Operator op, object value) => WithConditions((key, op, value));


        private Exception Deny(Methods method) => new ForbiddenException(
            NotAuthorized, $"{method} is not available for resource '{Resource.Name}'");

        /// <summary>
        /// Makes a GET request and serializes the output to an Excel workbook file. Returns a tuple with 
        /// the excel file as Stream and the number of non-header rows in the excel workbook.
        /// </summary>
        /// <returns></returns>
        public (Stream excel, long nrOfRows) GETExcel()
        {
            GET().GetExcelStream(Resource, out var excel, out var nrOfRows);
            return (excel, nrOfRows);
        }

        public IEnumerable<T> GET()
        {
            Prep();
            if (!GETAllowed) throw Deny(Methods.GET);
            return Evaluators<T>.SELECT(this) ?? new T[0];
        }

        public bool ANY()
        {
            Prep();
            if (!GETAllowed) throw Deny(Methods.GET);
            return Evaluators<T>.SELECT(this)?.Any() == true;
        }

        public long COUNT()
        {
            Prep();
            if (!GETAllowed) throw Deny(Methods.GET);
            return Evaluators<T>.OP_COUNT(this);
        }

        public int POST(Func<T> inserter)
        {
            if (!POSTAllowed) throw Deny(Methods.POST);
            return Evaluators<T>.App.POST(inserter, this);
        }

        public int POST(Func<IEnumerable<T>> inserter)
        {
            if (!POSTAllowed) throw Deny(Methods.POST);
            return Evaluators<T>.App.POST(inserter, this);
        }

        public int PATCH(Func<T, T> updater)
        {
            Prep();
            if (!PATCHAllowed) throw Deny(Methods.PATCH);
            var source = Evaluators<T>.SELECT(this)?.ToList();
            switch (source?.Count)
            {
                case null:
                case 0: return 0;
                case 1: return Evaluators<T>.App.PATCH(updater, source.First(), this);
                default: throw new AmbiguousMatchException(Resource);
            }
        }

        public int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater)
        {
            Prep();
            if (!PATCHAllowed) throw Deny(Methods.PATCH);
            var source = Evaluators<T>.SELECT(this)?.ToList();
            if (source?.Any() != true) return 0;
            return Evaluators<T>.App.PATCH(updater, source, this);
        }

        public int PUT(Func<T> inserter)
        {
            Prep();
            if (!PUTAllowed) throw Deny(Methods.PUT);
            var source = Evaluators<T>.SELECT(this);
            return Evaluators<T>.App.PUT(inserter, source, this);
        }

        public int PUT(Func<T> inserter, Func<T, T> updater)
        {
            Prep();
            if (!PUTAllowed) throw Deny(Methods.PUT);
            var source = Evaluators<T>.SELECT(this);
            return Evaluators<T>.App.PUT(inserter, updater, source, this);
        }

        public int DELETE(bool @unsafe = false)
        {
            Prep();
            if (!DELETEAllowed) throw Deny(Methods.DELETE);
            var source = Evaluators<T>.SELECT(this);
            if (source == null) return 0;
            if (!@unsafe)
            {
                var list = source.ToList();
                if (list.Count > 1)
                    throw new AmbiguousMatchException(Resource);
                source = list;
            }
            return Evaluators<T>.App.DELETE(source, this);
        }
    }
}