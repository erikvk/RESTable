using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Internal.Auth;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on a successful CORS preflight
    /// </summary>
    internal class Options : Success
    {
        private IResource Resource { get; }
        private bool HasResource => Resource != null;
        private IContentTypeProvider ContentTypeProvider { get; }
        public sealed override IRequest Request { get; }

        internal static Options Create(RequestParameters parameters)
        {
            var options = new Options(parameters);
            if (!parameters.IsValid)
                return options;
            var configuration = parameters.Context.Services.GetRequiredService<RESTableConfiguration>();
            var authenticator = parameters.Context.Services.GetRequiredService<Authenticator>();
            if (configuration.AllowAllOrigins)
                options.Headers.AccessControlAllowOrigin = "*";
            else if (Uri.TryCreate(parameters.Headers.Origin, UriKind.Absolute, out var origin) && authenticator.AllowedOrigins.Contains(origin))
            {
                options.Headers.AccessControlAllowOrigin = origin.ToString();
                options.Headers.Vary = "Origin";
            }
            else return options;
            if (options.HasResource)
                options.Headers.AccessControlAllowMethods = string.Join(", ", options.Resource.AvailableMethods);
            options.Headers.AccessControlMaxAge = "120";
            options.Headers.AccessControlAllowCredentials = "true";
            options.Headers.AccessControlAllowHeaders = "origin, content-type, accept, authorization, source, destination";
            return options;
        }

        private Options(RequestParameters parameters) : base(parameters)
        {
            Request = null;
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
            Resource = parameters.iresource;
            ContentTypeProvider = parameters.GetOutputContentTypeProvider();
        }

        public ISerializedResult Serialize(CancellationToken cancellationToken = new())
        {
            if (!HasResource)
                return new SerializedResult(this);
            var serializedResult = new SerializedResult(this);
            var stopwatch = Stopwatch.StartNew();
            var optionsBody = new OptionsBody(Resource.Name, Resource.ResourceKind, Resource.AvailableMethods);
            ContentTypeProvider.SerializeCollection(optionsBody.ToAsyncSingleton(), serializedResult.Body, null, cancellationToken);
            serializedResult.Body.TryRewind();
            Headers.Elapsed = stopwatch.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            return serializedResult;
        }
    }
}