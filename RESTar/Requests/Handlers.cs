﻿using System;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.View;
using Starcounter;
using static RESTar.RESTarMethods;
using static Starcounter.SessionOptions;
using static RESTar.Settings;
using ScRequest = Starcounter.Request;
using ScHandle = Starcounter.Handle;

namespace RESTar.Requests
{
    internal static class Handlers
    {
        internal static void Register(bool setupMenu)
        {
            var uri = _Uri + "{?}";
            ScHandle.GET(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.GET, GET));
            ScHandle.POST(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.POST, POST));
            ScHandle.PUT(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.PUT, PUT));
            ScHandle.PATCH(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.PATCH, PATCH));
            ScHandle.DELETE(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.DELETE, DELETE));
            ScHandle.OPTIONS(_Port, uri, (ScRequest r, string q) => CheckOrigin(r, q));

            if (!_ViewEnabled) return;

            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());

            ScHandle.GET(_Port, "/__restar/__page", () =>
            {
                if (Session.Current?.Data is View.Page)
                    return Session.Current.Data;
                Session.Current = Session.Current ?? new Session(PatchVersioning);
                return new View.Page {Session = Session.Current};
            });

            var viewUri = _ViewUri + "{?}";

            ScHandle.GET(_Port, viewUri, (ScRequest r, string query) =>
            {
                try
                {
                    Authenticator.UserCheck();
                    using (var request = new Request(r))
                    {
                        request.Populate(query, GET, Evaluators.VIEW);
                        if (!request.Resource.Viewable)
                            throw new ForbiddenException();
                        request.MethodCheck();
                        request.Evaluate();
                        var partial = (Json) request.GetResponse();
                        var master = Self.GET<View.Page>(_Port, "/__restar/__page");
                        master.CurrentPage = partial;
                        return master;
                    }
                }
                catch (Exception e)
                {
                    var master = Self.GET<View.Page>(_Port, "/__restar/__page");
                    var partial = master.CurrentPage as IRESTarView;
                    partial?.SetMessage(e.Message, MessageType.error);
                    return master;
                }
            });

            if (!setupMenu) return;
            ScHandle.GET(_Port, $"/{Application.Current.Name}", () =>
            {
                try
                {
                    Authenticator.UserCheck();
                    var partial = new Menu().Populate();
                    var master = Self.GET<View.Page>(_Port, "/__restar/__page");
                    master.CurrentPage = partial;
                    return master;
                }
                catch (Exception e)
                {
                    var master = Self.GET<View.Page>(_Port, "/__restar/__page");
                    var partial = master.CurrentPage as IRESTarView;
                    partial?.SetMessage(e.Message, MessageType.error);
                    return master;
                }
            });
        }

        private static Response CheckOrigin(ScRequest scRequest, string query)
        {
            try
            {
                var request = new Request(scRequest);
                request.Populate(query, default(RESTarMethods), null);
                var origin = request.ScRequest.Headers["Origin"].ToLower();
                if (RESTarConfig.AllowAllOrigins || RESTarConfig.AllowedOrigins.Contains(new Uri(origin)))
                    return Responses.AllowOrigin(origin, request.Resource.AvailableMethods);
                return Responses.Forbidden();
            }
            catch
            {
                return Responses.Forbidden();
            }
        }

        private static Response Handle(ScRequest scRequest, string query, Evaluator evaluator,
            RESTarMethods method)
        {
            Request request = null;
            try
            {
                using (request = new Request(scRequest))
                {
                    request.Authenticate();
                    request.Populate(query, method, evaluator);
                    request.MethodCheck();
                    request.GetRequestData();
                    request.Evaluate();
                    return request.GetResponse();
                }
            }

            #region Catch exceptions

            catch (Exception e)
            {
                Response errorResponse;
                ErrorCode errorCode;

                if (e is ForbiddenException) return Responses.Forbidden();
                if (e is NoContentException) return Responses.NoContent();

                if (e is SyntaxException)
                {
                    errorCode = ((SyntaxException) e).errorCode;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is FormatException)
                {
                    errorCode = ErrorCode.UnsupportedContentType;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is UnknownColumnException)
                {
                    errorCode = ErrorCode.UnknownColumnError;
                    errorResponse = Responses.NotFound(e);
                }
                else if (e is CustomEntityUnknownColumnException)
                {
                    errorCode = ErrorCode.UnknownColumnInGeneratedObjectError;
                    errorResponse = Responses.NotFound(e);
                }
                else if (e is AmbiguousColumnException)
                {
                    errorCode = ErrorCode.AmbiguousColumnError;
                    errorResponse = Responses.AmbiguousColumn((AmbiguousColumnException) e);
                }
                else if (e is SourceException)
                {
                    errorCode = ErrorCode.InvalidSourceDataError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is UnknownResourceException)
                {
                    errorCode = ErrorCode.UnknownResourceError;
                    errorResponse = Responses.NotFound(e);
                }
                else if (e is AmbiguousResourceException)
                {
                    errorCode = ErrorCode.AmbiguousResourceError;
                    errorResponse = Responses.AmbiguousResource((AmbiguousResourceException) e);
                }
                else if (e is InvalidInputCountException)
                {
                    errorCode = ErrorCode.DataSourceFormatError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is ExcelInputException)
                {
                    errorCode = ErrorCode.ExcelReaderError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is ExcelFormatException)
                {
                    errorCode = ErrorCode.ExcelReaderError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is JsonReaderException)
                {
                    errorCode = ErrorCode.JsonDeserializationError;
                    errorResponse = Responses.DeserializationError(scRequest.Body);
                }
                else if (e is DbException)
                {
                    errorCode = ErrorCode.DatabaseError;
                    errorResponse = Responses.DatabaseError(e);
                }
                else if (e is AbortedSelectorException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedInserterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedUpdaterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedDeleterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else
                {
                    errorCode = ErrorCode.UnknownError;
                    errorResponse = Responses.InternalError(e);
                }

                Error error = null;
                Error.ClearOld();
                Db.TransactAsync(() => error = new Error(errorCode, e, request));
                errorResponse.Headers["ErrorInfo"] = $"{_Uri}/{typeof(Error).FullName}/id={error.Id}";
                return errorResponse;
            }

            #endregion
        }
    }
}