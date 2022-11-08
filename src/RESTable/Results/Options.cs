using System;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Meta;
using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Returned to the client on a successful CORS preflight
/// </summary>
internal class Options : Success
{
    private Options(RequestParameters parameters) : base(parameters)
    {
        Request = parameters.Context.CreateRequest(parameters);
        StatusCode = HttpStatusCode.OK;
        StatusDescription = "OK";
        Resource = parameters.iresource;
        Stopwatch = Stopwatch.StartNew();
    }

    public IResource? Resource { get; }
    private Stopwatch Stopwatch { get; }
    public sealed override IRequest Request { get; }

    public override TimeSpan TimeElapsed => Stopwatch.Elapsed;

    public static Options Create(RequestParameters parameters)
    {
        var options = new Options(parameters);
        if (!parameters.IsValid)
            return options;
        var allowedOrigins = parameters.Context.GetRequiredService<IAllowedCorsOriginsFilter>();
        if (allowedOrigins is AllCorsOriginsAllowed)
        {
            options.Headers.AccessControlAllowOrigin = "*";
        }
        else if (Uri.TryCreate(parameters.Headers.Origin, UriKind.Absolute, out var origin) && allowedOrigins.IsAllowed(origin))
        {
            options.Headers.AccessControlAllowOrigin = origin.ToString();
            options.Headers.Vary = "Origin";
        }
        else
        {
            return options;
        }
        if (options.Resource is not null)
            options.Headers.AccessControlAllowMethods = string.Join(", ", options.Resource.AvailableMethods);
        options.Headers.AccessControlMaxAge = "120";
        options.Headers.AccessControlAllowCredentials = "true";
        options.Headers.AccessControlAllowHeaders = "origin, content-type, accept, authorization, source, destination";
        return options;
    }
}
