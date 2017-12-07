using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Serialization;
using Starcounter;
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
        public Origin Origin { get; private set; }
        public IResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public Stream Body { get; private set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        public ITarget<T> Target { get; private set; }
        private Response Response { get; set; }
        private Func<RESTRequest<T>, Response> Evaluator { get; set; }
        private string Source { get; set; }
        private string Destination { get; set; }
        private MimeType ContentType { get; set; }
        internal MimeType Accept { get; private set; }
        private string CORSOrigin { get; set; }
        private DataConfig InputDataConfig { get; set; }
        private DataConfig OutputDataConfig { get; set; }
        internal void Evaluate() => Response = Evaluator(this);
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

        internal void Populate(Args args, Methods method)
        {
            if (args.HasView)
            {
                if (!Resource.ViewDictionary.TryGetValue(args.View, out var view))
                    throw new UnknownViewException(args.View, Resource);
                Target = view;
            }
            Method = method;
            Evaluator = Operations<T>.REST.GetEvaluator(method);
            Source = args.Headers.SafeGet("Source");
            Destination = args.Headers.SafeGet("Destination");
            CORSOrigin = args.Headers.SafeGet("Origin");
            ContentType = MimeTypes.Match(args.ContentType);
            Accept = MimeTypes.Match(args.Accept);
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;
            args.NonReservedHeaders.ForEach(Headers.Add);
            if (args.HasConditions)
                Conditions = Condition<T>.Parse(args.Conditions, Target) ?? Conditions;
            if (args.HasMetaConditions)
                MetaConditions = MetaConditions.Parse(args.MetaConditions, Resource) ?? MetaConditions;
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
                        var request = new HttpRequest(Source) {Accept = ContentType.ToMimeString(), AuthToken = AuthToken};
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

        internal Response GetResponse()
        {
            ResponseHeaders.ForEach(h =>
            {
                if (h.Key.StartsWith("X-"))
                    Response.Headers[h.Key] = h.Value;
                else Response.Headers["X-" + h.Key] = h.Value;
            });
            if (AllowAllOrigins)
                Response.Headers["Access-Control-Allow-Origin"] = "*";
            else if (CORSOrigin != null)
                Response.Headers["Access-Control-Allow-Origin"] = CORSOrigin;
            switch (OutputDataConfig)
            {
                case DataConfig.Client: return Response;
                case DataConfig.External:
                    try
                    {
                        var request = new HttpRequest(Destination)
                        {
                            ContentType = Accept.ToMimeString(),
                            AuthToken = AuthToken,
                            Body = Response.StreamedBody
                        };
                        var response = request.GetResponse() ?? throw new DestinationException(request, "No response");
                        if (!response.IsSuccessStatusCode)
                            throw new DestinationException(request,
                                $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                        if (AllowAllOrigins)
                            Response.Headers["Access-Control-Allow-Origin"] = "*";
                        else if (CORSOrigin != null)
                            Response.Headers["Access-Control-Allow-Origin"] = CORSOrigin;
                        return (Response) response;
                    }
                    catch (HttpRequestException re)
                    {
                        throw new SyntaxException(InvalidDestination, $"{re.Message} in the Destination header");
                    }
                default: throw new ArgumentException();
            }
        }

        internal Response InsertedEntities(int count) => new Response
        {
            StatusCode = (ushort) Created,
            StatusDescription = "Created",
            Headers = {["RESTar-info"] = $"{count} entities inserted into resource '{Resource.Name}'"}
        };

        internal Response UpdatedEntities(int count) => new Response
        {
            StatusCode = (ushort) OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"{count} entities updated in resource '{Resource.Name}'"}
        };

        internal Response SafePostedEntities(int upd, int ins) => new Response
        {
            StatusCode = 200,
            StatusDescription = "OK",
            Headers =
            {
                ["RESTar-info"] = $"Updated {upd} and then inserted {ins} entities in resource '{Resource.Name}'"
            }
        };

        internal Response DeletedEntities(int count) => new Response
        {
            StatusCode = (ushort) OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"{count} entities deleted from resource '{Resource.Name}'"}
        };

        internal Response Report(Report report)
        {
            if (!report.SerializeReportJson(out var stream)) return NoContent;
            return new Response
            {
                StatusCode = (ushort) OK,
                StatusDescription = "OK",
                Headers = {["RESTar-info"] = $"Resource '{Resource.Name}'"},
                StreamedBody = stream
            };
        }

        public void Dispose()
        {
            if (Origin.IsExternal && AuthToken != null)
                AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}