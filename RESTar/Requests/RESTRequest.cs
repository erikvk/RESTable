using System;
using System.Collections.Generic;
using System.Net;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Error.NotFound;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static RESTar.Methods;

namespace RESTar.Requests
{
    //internal class RESTRequest<T> : IRequest<T>, IRequestInternal<T> where T : class
    //{
    //    public Methods Method { get; }
    //    public IEntityResource<T> Resource { get; }
    //    public Condition<T>[] Conditions { get; }
    //    public MetaConditions MetaConditions { get; }
    //    public Body Body { get; private set; }
    //    public Headers ResponseHeaders { get; }
    //    public ICollection<string> Cookies { get; }
    //    public IUriParameters UriParameters { get; }
    //    IEntityResource IRequest.Resource => Resource;
    //    public ITarget<T> Target { get; }
    //    internal Result Result { get; set; }
    //    private Func<RESTRequest<T>, Result> Evaluator { get; }
    //    internal string Source { get; }
    //    internal string Destination { get; }
    //    internal ResultFinalizer Finalizer { get; }
    //    private string CORSOrigin { get; }
    //    private DataConfig InputDataConfig { get; }
    //    private DataConfig OutputDataConfig { get; }
    //    public string TraceId { get; }
    //    public Client Client { get; }
    //    public Func<IEnumerable<T>> EntitiesGenerator { private get; set; }
    //    public IEnumerable<T> GetEntities() => EntitiesGenerator?.Invoke() ?? new T[0];
    //
    //    internal void Evaluate()
    //    {
    //        Result = Evaluator(this);
    //        Result.Cookies = Cookies;
    //        ResponseHeaders.ForEach(h => Result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
    //        if ((AllowAllOrigins ? "*" : CORSOrigin) is string allowedOrigin)
    //            Result.Headers["Access-Control-Allow-Origin"] = allowedOrigin;
    //    }
    //
    //    public Headers Headers { get; }
    //
    //    internal RESTRequest(IEntityResource<T> resource, RequestParameters requestParameters)
    //    {
    //        if (resource.IsInternal) throw new ResourceIsInternal(resource);
    //
    //        TraceId = requestParameters.TraceId;
    //        Client = requestParameters.Client;
    //
    //        Finalizer = requestParameters.ResultFinalizer;
    //        Resource = resource;
    //        Target = resource;
    //        Headers = requestParameters.Headers;
    //        ResponseHeaders = new Headers();
    //        Cookies = new List<string>();
    //        Conditions = new Condition<T>[0];
    //        MetaConditions = new MetaConditions();
    //        UriParameters = requestParameters.Uri;
    //        Method = requestParameters.Method;
    //        if (requestParameters.Uri.ViewName != null)
    //        {
    //            if (!Resource.ViewDictionary.TryGetValue(requestParameters.Uri.ViewName, out var view))
    //                throw new UnknownView(requestParameters.Uri.ViewName, Resource);
    //            Target = view;
    //        }
    //        Evaluator = Operations<T>.REST.GetEvaluator(Method);
    //        Source = requestParameters.Headers.SafeGet("Source");
    //        Destination = requestParameters.Headers.SafeGet("Destination");
    //        CORSOrigin = requestParameters.Headers.SafeGet("Origin");
    //        InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
    //        OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;
    //        Conditions = Condition<T>.Parse(requestParameters.Uri.Conditions, Target) ?? Conditions;
    //        MetaConditions = MetaConditions.Parse(requestParameters.Uri.MetaConditions, Resource) ?? MetaConditions;
    //        if (requestParameters.Headers.UnsafeOverride)
    //        {
    //            MetaConditions.Unsafe = true;
    //            requestParameters.Headers.UnsafeOverride = false;
    //        }
    //        if (Client.IsInternal) MetaConditions.Formatter = DbOutputFormat.Raw;
    //        this.MethodCheck();
    //        SetRequestData(requestParameters);
    //    }
    //
    //    internal void SetRequestData(RequestParameters requestParameters)
    //  {
    //      switch (InputDataConfig)
    //      {
    //          case DataConfig.Client:
    //              if (!requestParameters.Body.HasContent)
    //              {
    //                  if (Method == PATCH || Method == POST || Method == PUT)
    //                      throw new InvalidSyntax(NoDataSource, "Missing data source for method " + Method);
    //                  return;
    //              }
    //              Body = requestParameters.Body;
    //              break;
    //          case DataConfig.External:
    //              try
    //              {
    //                  var request = new HttpRequest(this, Source) {Accept = requestParameters.ContentType.ToString()};
    //                  if (request.Method != GET)
    //                      throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
    //                  var response = request.GetResponse() ?? throw new InvalidExternalSource(request, "No response");
    //                  if (response.StatusCode >= HttpStatusCode.BadRequest)
    //                      throw new InvalidExternalSource(request,
    //                          $"Status: {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
    //                  if (response.Body.CanSeek && response.Body.Length == 0)
    //                      throw new InvalidExternalSource(request, "Response was empty");
    //                  Body = new Body(response.Body.ToByteArray(), requestParameters.ContentType, requestParameters.InputContentTypeProvider);
    //                  break;
    //              }
    //              catch (HttpRequestException re)
    //              {
    //                  throw new InvalidSyntax(InvalidSource, $"{re.Message} in the Source header");
    //              }
    //      }
    //  }
}