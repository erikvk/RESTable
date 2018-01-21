using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Fail.BadRequest;
using RESTar.Results.Fail.Forbidden;
using RESTar.Results.Fail.NotFound;
using RESTar.Serialization;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static RESTar.Methods;

namespace RESTar.Requests
{
    internal class RESTRequest<T> : IRequest<T>, IDisposable where T : class
    {
        public Methods Method { get; }
        public IEntityResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; }
        public MetaConditions MetaConditions { get; }
        public Stream Body { get; private set; }
        public string AuthToken { get; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }
        public IUriParameters UriParameters { get; }
        IEntityResource IRequest.Resource => Resource;
        public ITarget<T> Target { get; }
        internal Result Result { get; set; }
        private Func<RESTRequest<T>, Result> Evaluator { get; }
        internal string Source { get; }
        internal string Destination { get; }
        private MimeTypeCode ContentType { get; }
        public MimeType Accept { get; }
        private string CORSOrigin { get; }
        private DataConfig InputDataConfig { get; }
        private DataConfig OutputDataConfig { get; }
        public string TraceId { get; }
        public TCPConnection TcpConnection { get; }

        internal void Evaluate()
        {
            Result = Evaluator(this);
            Result.Cookies = Cookies;
            ResponseHeaders.ForEach(h => Result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
            if ((AllowAllOrigins ? "*" : CORSOrigin) is string allowedOrigin)
                Result.Headers["Access-Control-Allow-Origin"] = allowedOrigin;
        }

        public T1 BodyObject<T1>() where T1 : class => Body?.Deserialize<T1>();
        public Headers Headers { get; }

        internal RESTRequest(IEntityResource<T> resource, Arguments arguments)
        {
            if (resource.IsInternal) throw new ResourceIsInternal(resource);

            TraceId = arguments.TraceId;
            TcpConnection = arguments.TcpConnection;

            Resource = resource;
            Target = resource;
            Headers = new Headers();
            ResponseHeaders = new Headers();
            Cookies = new List<string>();
            Conditions = new Condition<T>[0];
            MetaConditions = new MetaConditions();
            AuthToken = arguments.AuthToken;
            UriParameters = arguments.Uri;
            Method = (Methods) arguments.Action;
            if (arguments.Uri.ViewName != null)
            {
                if (!Resource.ViewDictionary.TryGetValue(arguments.Uri.ViewName, out var view))
                    throw new UnknownView(arguments.Uri.ViewName, Resource);
                Target = view;
            }
            Evaluator = Operations<T>.REST.GetEvaluator(Method);
            Source = arguments.Headers.SafeGet("Source");
            Destination = arguments.Headers.SafeGet("Destination");
            CORSOrigin = arguments.Headers.SafeGet("Origin");
            ContentType = arguments.ContentType.TypeCode;
            Accept = arguments.Accept;
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;
            arguments.CustomHeaders.ForEach(Headers.Put);
            Conditions = Condition<T>.Parse(arguments.Uri.Conditions, Target) ?? Conditions;
            MetaConditions = MetaConditions.Parse(arguments.Uri.MetaConditions, Resource) ?? MetaConditions;
            if (arguments.Headers.UnsafeOverride)
            {
                MetaConditions.Unsafe = true;
                arguments.Headers.UnsafeOverride = false;
            }
            if (TcpConnection.IsInternal) MetaConditions.Formatter = DbOutputFormat.Raw;
            this.MethodCheck();
            SetRequestData(arguments.BodyBytes);
        }

        internal void SetRequestData(byte[] bodyBytes)
        {
            switch (InputDataConfig)
            {
                case DataConfig.Client:
                    if (bodyBytes == null && (Method == PATCH || Method == POST || Method == PUT))
                        throw new InvalidSyntax(NoDataSource, "Missing data source for method " + Method);
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
                            throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                        var response = request.GetResponse(this) ?? throw new InvalidExternalSource(request, "No response");
                        if (!response.IsSuccessStatusCode)
                            throw new InvalidExternalSource(request,
                                $"Status: {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                        if (response.Body.CanSeek && response.Body.Length == 0)
                            throw new InvalidExternalSource(request, "Response was empty");
                        Body = response.Body;
                        break;
                    }
                    catch (HttpRequestException re)
                    {
                        throw new InvalidSyntax(InvalidSource, $"{re.Message} in the Source header");
                    }
            }

            switch (ContentType)
            {
                case MimeTypeCode.Json: break;
                case MimeTypeCode.Excel:
                    Body.SerializeInputExcel(Method, out var json);
                    Body = json;
                    break;
                case MimeTypeCode.Unsupported:
                    break;
            }
        }

        public void Dispose()
        {
            if (TcpConnection.IsExternal && AuthToken != null)
                AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}