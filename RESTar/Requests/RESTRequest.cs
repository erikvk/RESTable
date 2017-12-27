using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Protocols;
using RESTar.Serialization;
using static System.Net.HttpStatusCode;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static RESTar.Methods;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class RESTRequest<T> : IRequest<T>, IDisposable where T : class
    {
        public Methods Method { get; private set; }
        public Origin Origin { get; }
        public IResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public Stream Body { get; private set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        public IUriParameters UriParameters { get; private set; }

        // public IUriParameters GetNextPage()
        // {
        //     var uriParams = new UriParameters
        //     {
        //         ResourceSpecifier = Resource.Name,
        //         ViewName = Target is View<T> view ? view.Name : null,
        //     };
        //     Conditions.ForEach(c => uriParams.UriConditions.Add(new UriCondition(c.Key, c.Operator, c.Value)));
        //     MetaConditions.ForEach(c => uriParams.UriConditions.Add(new UriCondition(c.Key, c.Operator, c.Value)));
        // }

        IResource IRequest.Resource => Resource;
        public ITarget<T> Target { get; private set; }
        internal Result Result { get; set; }
        private Func<RESTRequest<T>, Result> Evaluator { get; set; }
        private string Source { get; set; }
        private string Destination { get; set; }
        private MimeType ContentType { get; set; }
        public MimeType Accept { get; private set; }
        private string CORSOrigin { get; set; }
        private DataConfig InputDataConfig { get; set; }
        private DataConfig OutputDataConfig { get; set; }
        private ResultFinalizer ResultFinalizer { get; set; }

        internal void Evaluate()
        {
            Result = Evaluator(this);
            Result.ExternalDestination = Destination;
            Result.ContentType = MimeTypes.GetString(Accept);
            ResponseHeaders.ForEach(h =>
            {
                if (h.Key.StartsWith("X-"))
                    Result.Headers[h.Key] = h.Value;
                else Result.Headers["X-" + h.Key] = h.Value;
            });
            if (AllowAllOrigins)
                Result.Headers["Access-Control-Allow-Origin"] = "*";
            else if (CORSOrigin != null)
                Result.Headers["Access-Control-Allow-Origin"] = CORSOrigin;
        }

        public T1 BodyObject<T1>() where T1 : class => Body?.Deserialize<T1>();
        public Headers Headers { get; }

        internal RESTRequest(IResource<T> resource, Origin origin)
        {
            if (resource.IsInternal) throw new ResourceIsInternalException(resource);
            Resource = resource;
            Target = resource;
            Headers = new Headers();
            ResponseHeaders = new Dictionary<string, string>();
            Conditions = new Condition<T>[0];
            MetaConditions = new MetaConditions();
            Origin = origin;
        }

        internal void Populate(Arguments arguments, Methods method)
        {
            if (arguments.ViewName != null)
            {
                if (!Resource.ViewDictionary.TryGetValue(arguments.ViewName, out var view))
                    throw new UnknownViewException(arguments.ViewName, Resource);
                Target = view;
            }

            UriParameters = arguments;
            Method = method;
            Evaluator = Operations<T>.REST.GetEvaluator(method);
            Source = arguments.Headers.SafeGet("Source");
            Destination = arguments.Headers.SafeGet("Destination");
            CORSOrigin = arguments.Headers.SafeGet("Origin");
            ContentType = MimeTypes.Match(arguments.ContentType);
            Accept = MimeTypes.Match(arguments.Accept);
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;
            arguments.CustomHeaders.ForEach(Headers.Add);
            Conditions = Condition<T>.Parse(arguments.UriConditions, Target) ?? Conditions;
            MetaConditions = MetaConditions.Parse(arguments.UriMetaConditions, Resource) ?? MetaConditions;
            ResultFinalizer = arguments.ResultFinalizer;
            if (Origin.IsInternal) MetaConditions.Formatter = DbOutputFormat.Raw;
        }

        internal void SetRequestData(byte[] bodyBytes)
        {
            switch (InputDataConfig)
            {
                case DataConfig.Client:
                    if (bodyBytes == null && (Method == PATCH || Method == POST || Method == PUT))
                        throw new SyntaxException(NoDataSource, "Missing data source for method " + Method);
                    if (bodyBytes == null) return;
                    Body = new MemoryStream(bodyBytes);
                    break;
                case DataConfig.External:
                    try
                    {
                        var request = new HttpRequest(Source)
                        {
                            Accept = ContentType.ToMimeString(),
                            AuthToken = AuthToken
                        };
                        if (request.Method != GET)
                            throw new SyntaxException(InvalidSource, "Only GET is allowed in Source headers");
                        var response = request.GetResponse() ?? throw new SourceException(request, "No response");
                        if (!response.IsSuccessStatusCode)
                            throw new SourceException(request,
                                $"Status: {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                        if (response.Body.CanSeek && response.Body.Length == 0)
                            throw new SourceException(request, "Response was empty");
                        Body = response.Body;
                        break;
                    }
                    catch (HttpRequestException re)
                    {
                        throw new SyntaxException(InvalidSource, $"{re.Message} in the Source header");
                    }
            }

            if (ContentType == MimeType.Excel)
            {
                Body.SerializeInputExcel(Method, out var jsonStream);
                Body = jsonStream;
            }
        }

        internal IFinalizedResult GetFinalizedResult()
        {
            try
            {
                return ResultFinalizer(Result);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException<T>(e, this);
            }
        }

        internal Result Entities(IEnumerable<dynamic> entities) => new Result(this)
        {
            StatusCode = OK,
            StatusDescription = "OK",
            Entities = entities
        };

        internal Result Report(Report report)
        {
            if (!report.TryGetReportJsonStream(out var stream)) return Results.NoContent;
            return new Result(this)
            {
                StatusCode = OK,
                StatusDescription = "OK",
                Headers =
                {
                    ["RESTar-info"] = $"Resource '{Resource.Name}'"
                },
                Body = stream
            };
        }

        internal Result InsertedEntities(int count) => new Result(this)
        {
            StatusCode = Created,
            StatusDescription = "Created",
            Headers =
            {
                ["RESTar-info"] = $"{count} entities inserted into resource '{Resource.Name}'"
            }
        };

        internal Result UpdatedEntities(int count) => new Result(this)
        {
            StatusCode = OK,
            StatusDescription = "OK",
            Headers =
            {
                ["RESTar-info"] = $"{count} entities updated in resource '{Resource.Name}'"
            }
        };

        internal Result SafePostedEntities(int upd, int ins) => new Result(this)
        {
            StatusCode = OK,
            StatusDescription = "OK",
            Headers =
            {
                ["RESTar-info"] = $"Updated {upd} and then inserted {ins} entities in resource '{Resource.Name}'"
            }
        };

        internal Result DeletedEntities(int count) => new Result(this)
        {
            StatusCode = OK,
            StatusDescription = "OK",
            Headers =
            {
                ["RESTar-info"] = $"{count} entities deleted from resource '{Resource.Name}'"
            }
        };

        public void Dispose()
        {
            if (Origin.IsExternal && AuthToken != null)
                AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}