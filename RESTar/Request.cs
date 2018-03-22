using System;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Success;
using static RESTar.Methods;
using static RESTar.RESTarConfig;

namespace RESTar
{
    public static class Request<T> where T : class
    {
        public static IRequest<T> Create(ITraceable trace, Methods method, string protocolId = null)
        {
            var parameters = new RequestParameters(trace, method, protocolId);
        }
    }

    public static class Request
    {
        private static IRequest MakeRequest<T>(IResource<T> resource, RequestParameters requestParameters) where T : class
        {
            return Requests.ParsedRequest<T>.Create(resource, requestParameters);
        }
        
        public static IRequest Create(ITraceable trace, Methods method, ref string uri, byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();
            if (trace == null) throw new Untraceable();
            var parameters = new RequestParameters(trace, method, ref uri, body, headers);
            parameters.Authenticate();
            if (!parameters.IsValid)
                return new InvalidParametersRequest(parameters);
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