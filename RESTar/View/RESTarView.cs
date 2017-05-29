using System.IO;
using RESTar.Operations;
using Starcounter;
using static RESTar.ErrorCode;
using static RESTar.Internal.Authenticator;
using static RESTar.View.MessageType;
using IResource = RESTar.Internal.IResource;
using Request = RESTar.Requests.Request;

namespace RESTar.View
{
    public enum MessageType
    {
        info,
        error,
        warning
    }

    internal interface IRESTarView
    {
        void SetMessage(string message, ErrorCode errorCode, MessageType messageType);
    }

    public abstract class RESTarView<TData> : Json, IRESTarView
    {
        protected abstract void SetHtml(string html);
        protected abstract void SetResourceName(string resourceName);
        protected abstract void SetResourcePath(string resourceName);
        public abstract void SetMessage(string message, ErrorCode errorCode, MessageType messageType);

        internal Request Request { get; private set; }
        internal IResource Resource => Request.Resource;
        protected abstract string HtmlMatcher { get; }
        protected TData RESTarData { get; private set; }

        protected void POST(string json)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.POST))
                Evaluators.POST(json, Request);
            else SetMessage($"You are not allowed to insert into the '{Resource}' resource", NotAuthorized, error);
        }

        protected void PATCH(string json)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.PATCH))
                Evaluators.PATCH(RESTarData, json, Request);
            else SetMessage($"You are not allowed to update the '{Resource}' resource", NotAuthorized, error);
        }

        protected void DELETE(object item)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.DELETE))
                Evaluators.DELETE(item, Request);
            else SetMessage($"You are not allowed to delete from the '{Resource}' resource", NotAuthorized, error);
        }

        protected bool MethodAllowed(RESTarMethods method) => MethodCheck(method, Resource, Request.AuthToken);

        internal virtual RESTarView<TData> Populate(Request request, TData data)
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