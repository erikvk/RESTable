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
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;
using RESTar.Serialization;

namespace RESTar
{
    /// <inheritdoc cref="IRequest{T}" />
    /// <summary>
    /// An internal RESTar request for entity resources, that bypasses all network and authentication components
    /// </summary>
    /// <typeparam name="T">The entity resource type</typeparam>
    public class Request<T> : IRequest<T>, IRequestInternal<T> where T : class
    {
        #region Private and explicit members

        private Condition<T>[] _conditions;
        Body IRequest.Body => _body;
        internal string AuthToken { get; set; }
        string IRequest.AuthToken => AuthToken;
        Headers IRequest.ResponseHeaders { get; } = new Headers();
        ICollection<string> IRequest.Cookies { get; } = new List<string>();
        IUriParameters IRequest.UriParameters => throw new InvalidOperationException();
        IEntityResource IRequest.Resource => Resource;
        TCPConnection ITraceable.TcpConnection { get; } = TCPConnection.Internal;
        Methods IRequest.Method => 0;
        Headers IRequest.Headers => RequestHeaders;
        string ITraceable.TraceId => null;
        private Func<IEnumerable<T>> EntitiesGenerator { get; set; }
        IEnumerable<T> IRequest<T>.GetEntities() => EntitiesGenerator?.Invoke() ?? new T[0];

        Func<IEnumerable<T>> IRequestInternal<T>.EntitiesGenerator
        {
            set => EntitiesGenerator = value;
        }

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
        private Body _body;

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
                var (WhereString, Values) = sql.MakeWhereClause(out var assignments);
                SelectQuery = $"{StarcounterOperations<T>.SELECT}{WhereString}";
                CountQuery = $"{StarcounterOperations<T>.COUNT}{WhereString}";
                SqlValues = Values;
                ValuesAssignments = assignments;
            }
        }

        #endregion

        /// <summary>
        /// The body to include in the request. Set as .NET object, for example an anonymous type.
        /// </summary>
        public object Body
        {
            set => _body = new Body(Serializers.Json.SerializeToBytes(value), "application/json", Serializers.Json);
        }

        /// <inheritdoc />
        public IEntityResource<T> Resource { get; }

        /// <inheritdoc />
        public ITarget<T> Target { get; }

        /// <inheritdoc />
        public Condition<T>[] Conditions
        {
            get => _conditions;
            set
            {
                _conditions = value ?? new Condition<T>[0];
                if (ScSql) BuildSQL();
            }
        }

        /// <inheritdoc />
        public MetaConditions MetaConditions { get; }

        /// <summary>
        /// The headers to include in the request
        /// </summary>
        public Headers RequestHeaders { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="RESTar.Request{T}"/> class
        /// </summary>
        /// <param name="conditions"></param>
        public Request(params Condition<T>[] conditions)
        {
            if (!RESTarConfig.Initialized)
                throw new NotInitialized();
            Resource = EntityResource<T>.Get;
            Target = Resource;
            MetaConditions = new MetaConditions {Unsafe = true};
            Conditions = conditions;
            this.Authenticate();
            ScSql = Resource.Provider == typeof(StarcounterResourceProvider).GetProviderId();
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

        /// <summary>
        /// Uses the given conditions, and returns a reference to the request.
        /// </summary>
        public Request<T> WithConditions(IEnumerable<Condition<T>> conditions)
        {
            if (conditions is Condition<T>[] arr)
                Conditions = arr;
            else Conditions = conditions?.ToArray();
            return this;
        }

        /// <summary>
        /// Uses the given conditions, and returns a reference to the request.
        /// </summary>
        public Request<T> WithConditions(params Condition<T>[] conditions)
        {
            Conditions = conditions;
            return this;
        }

        /// <summary>
        /// Uses the given conditions, and returns a reference to the request.
        /// </summary>
        public Request<T> WithConditions(params (string key, Operators op, object value)[] conditions)
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

        /// <summary>
        /// Uses the given conditions, and returns a reference to the request.
        /// </summary>
        public Request<T> WithConditions(string key, Operators op, object value) => WithConditions((key, op, value));

        /// <summary>
        /// Makes a GET request and serializes the output to an Excel workbook file. Returns a tuple with 
        /// the excel file as Stream and the number of non-header rows in the excel workbook.
        /// </summary>
        /// <returns></returns>
        public (Stream excel, ulong nrOfRows) GETExcel()
        {
            var stream = Serializers.Excel.SerializeCollection("excel", GET(), this, out var count);
            return (stream, count);
        }

        /// <summary>
        /// Gets all entities in the resource for which the condition(s) hold.
        /// </summary>
        public IEnumerable<T> GET()
        {
            Prep();
            if (!GETAllowed) throw new MethodUnavailable(Methods.GET, Resource);
            return Operations<T>.SELECT(this) ?? new T[0];
        }

        /// <summary>
        /// Returns true if and only if there is at least one entity in the resource for which the condition(s) hold.
        /// </summary>
        public bool ANY()
        {
            Prep();
            if (!GETAllowed) throw new MethodUnavailable(Methods.GET, Resource);
            return Operations<T>.SELECT(this)?.Any() == true;
        }

        /// <summary>
        /// Returns the number of entities in the resource for which the condition(s) hold.
        /// </summary>
        public long COUNT()
        {
            Prep();
            if (!GETAllowed) throw new MethodUnavailable(Methods.GET, Resource);
            return Operations<T>.OP_COUNT(this);
        }

        /// <summary>
        /// Inserts an entity into the resource
        /// </summary>
        /// <returns>The number of entities affected</returns>
        public int POST(Func<T> inserter)
        {
            if (!POSTAllowed) throw new MethodUnavailable(Methods.POST, Resource);
            return Operations<T>.App.POST(inserter, this);
        }

        /// <summary>
        /// Inserts a collection of entities into the resource
        /// </summary>
        /// <returns>The number of entities affected</returns>
        public int POST(Func<IEnumerable<T>> inserter)
        {
            if (!POSTAllowed) throw new MethodUnavailable(Methods.POST, Resource);
            return Operations<T>.App.POST(inserter, this);
        }

        /// <summary>
        /// Updates an entity in the resource
        /// </summary>
        /// <returns>The number of entities affected</returns>
        public int PATCH(Func<T, T> updater)
        {
            Prep();
            if (!PATCHAllowed) throw new MethodUnavailable(Methods.PATCH, Resource);
            var source = Operations<T>.SELECT(this)?.ToList();
            switch (source?.Count)
            {
                case null:
                case 0: return 0;
                case 1: return Operations<T>.App.PATCH(updater, source.First(), this);
                default: throw new AmbiguousMatch(Resource);
            }
        }

        /// <summary>
        /// Updates a collection of entities in the resource
        /// </summary>
        /// <returns>The number of entities affected</returns>
        public int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater)
        {
            Prep();
            if (!PATCHAllowed) throw new MethodUnavailable(Methods.PATCH, Resource);
            var source = Operations<T>.SELECT(this)?.ToList();
            if (source?.Any() != true) return 0;
            return Operations<T>.App.PATCH(updater, source, this);
        }

        /// <summary>
        /// Inserts an entity into the resource if the conditions do not match any existing entities
        /// </summary>
        /// <returns>The number of entities affected</returns>
        public int PUT(Func<T> inserter)
        {
            Prep();
            if (!PUTAllowed) throw new MethodUnavailable(Methods.PUT, Resource);
            var source = Operations<T>.SELECT(this);
            return Operations<T>.App.PUT(inserter, source, this);
        }

        /// <summary>
        /// Inserts an entity into the resource if the conditions do not match any single existing entity. 
        /// Otherwise updates the matched entity. If many entities are matched, throws an <see cref="AmbiguousMatch"/> exception.
        /// </summary>
        /// <returns>The number of entities affected</returns>
        public int PUT(Func<T> inserter, Func<T, T> updater)
        {
            Prep();
            if (!PUTAllowed) throw new MethodUnavailable(Methods.PUT, Resource);
            var source = Operations<T>.SELECT(this);
            return Operations<T>.App.PUT(inserter, updater, source, this);
        }

        /// <summary>
        /// Deletes the selected entity or entities. To enable deletion of multiple entities, set the 
        /// unsafe parameter to true.
        /// </summary>
        /// <param name="unsafe">Should deletion of multiple entities be allowed?</param>
        /// <returns>The number of entities affected</returns>
        public int DELETE(bool @unsafe = false)
        {
            Prep();
            if (!DELETEAllowed) throw new MethodUnavailable(Methods.DELETE, Resource);
            var source = Operations<T>.SELECT(this);
            if (source == null) return 0;
            if (!@unsafe)
            {
                var list = source.ToList();
                if (list.Count > 1)
                    throw new AmbiguousMatch(Resource);
                source = list;
            }

            return Operations<T>.App.DELETE(source, this);
        }
    }
}