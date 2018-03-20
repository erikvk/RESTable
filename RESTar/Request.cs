using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Success;
using static RESTar.Methods;
using static RESTar.RESTarConfig;

namespace RESTar
{
    internal class InvalidParametersRequest : IRequest, IRequestInternal
    {
        private Exception Error { get; }

        public void Dispose() { }

        #region Logable

        private ILogable LogItem => RequestParameters;
        LogEventType ILogable.LogEventType => LogItem.LogEventType;
        string ILogable.LogMessage => LogItem.LogMessage;
        string ILogable.LogContent => LogItem.LogContent;

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        public string Destination { get; }

        public IFinalizedResult HandleError(Exception exception) => throw new NotImplementedException();

        string ILogable.HeadersStringCache
        {
            get => LogItem.HeadersStringCache;
            set => LogItem.HeadersStringCache = value;
        }

        bool ILogable.ExcludeHeaders => LogItem.ExcludeHeaders;

        #endregion

        public RequestParameters RequestParameters { get; }
        public Methods Method => RequestParameters.Method;
        public string TraceId => RequestParameters.TraceId;
        public Client Client => RequestParameters.Client;
        public IUriParameters UriParameters => RequestParameters.Uri;
        public Headers Headers => RequestParameters.Headers;

        public IEntityResource Resource => RequestParameters.IResource as IEntityResource;
        public MetaConditions MetaConditions { get; }
        public Body Body { get; set; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }

        public IResult GetResult() => RESTarError.GetResult(Error, RequestParameters);

        public bool IsValid { get; }

        internal InvalidParametersRequest(ITraceable trace, RequestParameters parameters)
        {
            IsValid = false;
            RequestParameters = parameters;
            Error = parameters.Error;
            MetaConditions = null;
            Body = parameters.Body;
            ResponseHeaders = null;
            Cookies = null;
        }
    }


    public static class Request
    {
        private static IRequest MakeRequest<T>(IResource<T> resource, RequestParameters requestParameters) where T : class
        {
            return Request<T>.Create(resource, requestParameters);
        }

        private static bool ValidateRequest<T>(IResource<T> resource, RequestParameters requestParameters) where T : class
        {
            return Request<T>.Validate(resource, requestParameters);
        }

        public static IRequest Create(ITraceable trace, Methods method, ref string uri, byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();
            if (trace == null) throw new Untraceable();
            var parameters = new RequestParameters(trace, method, ref uri, body, headers);
            parameters.Authenticate();
            return MakeRequest((dynamic) parameters.IResource, parameters);
        }

        public static IResult GET(ITraceable trace, ref string uri, byte[] body = null, Headers headers = null)
        {
            var request = Create(trace, Methods.GET, ref uri, body, headers);
            return request.GetResult();
        }

        public static IResult Custom(ITraceable trace, Methods method, ref string uri, byte[] body = null, Headers headers = null)
        {
            var request = Create(trace, method, ref uri, body, headers);
            return request.GetResult();
        }

        public static IFinalizedResult CheckOrigin(ITraceable trace, ref string uri, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();
            if (trace == null) throw new Untraceable();
            var parameters = new RequestParameters(trace, OPTIONS, ref uri, null, headers);
            var origin = parameters.Headers["Origin"];
            if (!parameters.IsValid || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
                return new InvalidOrigin();
            if (AllowAllOrigins || AllowedOrigins.Contains(originUri))
                return new AcceptOrigin(origin, parameters);
            return new InvalidOrigin();
        }

        public static bool IsValid(ITraceable trace, ref string uri, out RESTarError error)
        {
            var parameters = new RequestParameters(trace, (Methods) (-1), ref uri, null, null);
            parameters.Authenticate();
            if (parameters.Error != null)
            {
                error = RESTarError.GetError(parameters.Error);
                return false;
            }
            IRequest request = MakeRequest((dynamic) parameters.IResource, parameters);
            if (request.IsValid)
            {
                error = null;
                return true;
            }
            error = request.GetResult() as RESTarError;
            return false;
        }
    }
}