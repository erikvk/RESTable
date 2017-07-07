using System.IO;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Internal.Authenticator;
using static RESTar.View.MessageType;
using IResource = RESTar.Internal.IResource;

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// </summary>
        info,

        /// <summary>
        /// </summary>
        error,

        /// <summary>
        /// </summary>
        warning
    }

    internal interface IRESTarView
    {
        void SetMessage(string message, ErrorCodes errorCode, MessageType messageType);
    }

    /// <summary>
    /// </summary>
    public abstract class RESTarView<TData> : Json, IRESTarView
    {
        /// <summary>
        /// </summary>
        protected abstract void SetHtml(string html);

        /// <summary>
        /// </summary>
        protected abstract void SetResourceName(string resourceName);

        /// <summary>
        /// </summary>
        protected abstract void SetResourcePath(string resourceName);

        /// <summary>
        /// </summary>
        public abstract void SetMessage(string message, ErrorCodes errorCode, MessageType messageType);

        internal Requests.RESTRequest Request { get; private set; }
        internal IResource Resource => Request.Resource;

        /// <summary>
        /// </summary>
        protected abstract string HtmlMatcher { get; }

        /// <summary>
        /// </summary>
        protected TData RESTarData { get; private set; }

        /// <summary>
        /// </summary>
        protected bool Success;

        /// <summary>
        /// </summary>
        protected void POST(string json)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.POST))
            {
                try
                {
                    RESTEvaluators.POST(json, Request);
                    Success = true;
                }
                catch (AbortedInserterException e)
                {
                    SetMessage(e.InnerException?.Message ?? e.Message, e.ErrorCode, error);
                }
            }
            else SetMessage($"You are not allowed to insert into the '{Resource}' resource", NotAuthorized, error);
        }

        /// <summary>
        /// </summary>
        protected void PATCH(string json)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.PATCH))
            {
                try
                {
                    RESTEvaluators.PATCH(RESTarData, json, Request);
                    Success = true;
                }
                catch (AbortedUpdaterException e)
                {
                    SetMessage(e.InnerException?.Message ?? e.Message, e.ErrorCode, error);
                }
            }
            else SetMessage($"You are not allowed to update the '{Resource}' resource", NotAuthorized, error);
        }

        /// <summary>
        /// </summary>
        protected void DELETE(object item)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.DELETE))
            {
                try
                {
                    RESTEvaluators.DELETE(item, Request);
                    Success = true;
                }
                catch (AbortedDeleterException e)
                {
                    SetMessage(e.InnerException?.Message ?? e.Message, e.ErrorCode, error);
                }
            }
            else SetMessage($"You are not allowed to delete from the '{Resource}' resource", NotAuthorized, error);
        }

        /// <summary>
        /// </summary>
        protected bool MethodAllowed(RESTarMethods method) => MethodCheck(method, Resource, Request.AuthToken);

        internal virtual RESTarView<TData> Populate(Requests.RESTRequest request, TData data)
        {
            Request = request;
            SetResourceName(Resource.Alias ?? Resource.Name);
            SetResourcePath($"/{Application.Current.Name}/{Resource.Alias ?? Resource.Name}");
            var wd = Application.Current.WorkingDirectory;
            var exists = File.Exists($"{wd}/wwwroot/resources/{HtmlMatcher}");
            if (!exists) exists = File.Exists($"{wd}/../wwwroot/resources/{HtmlMatcher}");
            if (!exists) throw new NoHtmlException(Resource, HtmlMatcher);
            SetHtml($"/resources/{HtmlMatcher}");
            if (data == null)
                SetMessage("No entities found maching query", NoError, info);
            RESTarData = data;
            return this;
        }
    }
}