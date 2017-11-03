﻿using System;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
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
        public Origin Origin { get; }
        public IResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public Stream Body { get; private set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        internal Request ScRequest { get; }
        private Response Response { get; set; }
        private Func<RESTRequest<T>, Response> Evaluator { get; set; }
        private string Source { get; set; }
        private string Destination { get; set; }
        private MimeType ContentType { get; set; }
        internal MimeType Accept { get; set; }
        private string CORSOrigin { get; set; }
        private DataConfig InputDataConfig { get; set; }
        private DataConfig OutputDataConfig { get; set; }
        internal void Evaluate() => Response = Evaluator(this);

        internal RESTRequest(IResource<T> resource, Request scRequest)
        {
            if (resource.IsInternal) throw new ResourceIsInternalException(resource);
            Resource = resource;
            ScRequest = scRequest;
            Origin = new Origin(scRequest);
            ResponseHeaders = new Dictionary<string, string>();
            Conditions = new Condition<T>[0];
            MetaConditions = new MetaConditions();
        }

        internal void Populate(Args args, Methods method)
        {
            Method = method;
            Evaluator = Evaluators<T>.REST.GetEvaluator(method);
            Source = ScRequest.Headers["Source"];
            Destination = ScRequest.Headers["Destination"];
            CORSOrigin = ScRequest.Headers["Origin"];
            ContentType = MimeTypes.Match(ScRequest.ContentType);
            Accept = MimeTypes.Match(ScRequest.PreferredMimeTypeString);
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;
            if (args.HasConditions)
                Conditions = Condition<T>.Parse(args.Conditions, Resource) ?? Conditions;
            if (args.HasMetaConditions)
                MetaConditions = MetaConditions.Parse(args.MetaConditions, Resource) ?? MetaConditions;
        }

        internal void SetRequestData()
        {
            #region Resolve data source

            switch (InputDataConfig)
            {
                case DataConfig.Client:
                    if (ScRequest.Body == null && (Method == PATCH || Method == POST || Method == PUT))
                        throw new SyntaxException(NoDataSource, "Missing data source for method " + Method);
                    if (ScRequest.Body == null) return;
                    Body = new MemoryStream(ScRequest.BodyBytes);
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
                                $"Status: {response.StatusCode} - {response.StatusDescription}. {response.Headers["RESTar-info"]}");
                        if (response.StreamedBody.CanSeek && response.StreamedBody.Length == 0)
                            throw new SourceException(request, "Response was empty");
                        Body = response.StreamedBody;
                        break;
                    }
                    catch (HttpRequestException re)
                    {
                        throw new SyntaxException(InvalidSource, $"{re.Message} in the Source header");
                    }
            }

            #endregion

            #region Check data format

            switch (ContentType)
            {
                case MimeType.XML: throw new FormatException("XML is only supported as output format");
                case MimeType.Excel:
                    MemoryStream jsonStream;
                    using (var stream = Body)
                    {
                        var dataTable = ExcelReaderFactory.CreateOpenXmlReader(stream).GetDataSet().Tables[0];
                        if (Method != POST && dataTable.Rows.Count > 1)
                            throw new InvalidInputCountException();
                        dataTable.GetJsonStreamFromExcel(out jsonStream);
                    }
                    jsonStream.Seek(0, SeekOrigin.Begin);
                    Body = jsonStream;
                    return;
            }

            #endregion
        }

        internal Response GetResponse()
        {
            ResponseHeaders.ForEach(h => Response.Headers["X-" + h.Key] = h.Value);
            Response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (CORSOrigin ?? "null");
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
                                $"Received {response.StatusCode} - {response.StatusDescription}. {response.Headers["RESTar-info"]}");
                        response.Headers["Access-Control-Allow-Origin"] =
                            AllowAllOrigins ? "*" : (CORSOrigin ?? "null");
                        if (response.StreamedBody?.CanSeek == false)
                        {
                            response.BodyBytes = response.StreamedBody.ToByteArray();
                            response.StreamedBody = null;
                        }
                        return response;
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

        internal Response EntityCount(long Count) => new Response
        {
            StatusCode = (ushort) OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"Resource '{Resource.Name}'"},
            Body = new {Count}.Serialize()
        };

        public void Dispose()
        {
            if (ScRequest.IsExternal && AuthToken != null)
                AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}