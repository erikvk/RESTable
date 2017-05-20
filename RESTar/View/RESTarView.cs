using System.IO;
using RESTar.Operations;
using Starcounter;
using static RESTar.Internal.Authenticator;
using IResource = RESTar.Internal.IResource;

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
        void SetMessage(string message, MessageType messageType);
    }

    public abstract class RESTarView<TData> : Json, IRESTarView
    {
        protected abstract void SetHtml(string html);
        protected abstract void SetResourceName(string resourceName);
        protected abstract void SetResourcePath(string resourceName);
        public abstract void SetMessage(string message, MessageType messageType);

        internal IRequest Request { get; private set; }
        internal IResource Resource => Request.Resource;
        protected abstract string HtmlMatcher { get; }
        protected abstract string DefaultHtml { get; }
        protected TData RESTarData { get; private set; }

        protected void POST(string json)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.POST))
                Evaluators.POST(json, Request);
            else SetMessage($"You are not allowed to insert into the '{Resource}' resource", MessageType.error);
        }

        protected void PATCH(string json)
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.PATCH))
                Evaluators.PATCH(RESTarData, json, Request);
            else SetMessage($"You are not allowed to update the '{Resource}' resource", MessageType.error);
        }

        protected void DELETE()
        {
            UserCheck();
            if (MethodAllowed(RESTarMethods.DELETE))
                Evaluators.DELETE(RESTarData, Request);
            else SetMessage($"You are not allowed to delete from the '{Resource}' resource", MessageType.error);
        }

        protected bool MethodAllowed(RESTarMethods method) => MethodCheck(method, Resource, Request.AuthToken);

        internal virtual RESTarView<TData> Populate(IRequest request, TData data)
        {
            Request = request;
            SetResourceName(Resource.Alias ?? Resource.Name);
            SetResourcePath($"{Settings._ViewUri}/{Resource.Alias ?? Resource.Name}");
            var wd = Application.Current.WorkingDirectory;
            var exists = File.Exists($"{wd}/wwwroot{HtmlMatcher}");
            SetHtml(exists ? HtmlMatcher : DefaultHtml);
            if (data == null)
                SetMessage("No entities found maching query", MessageType.info);
            RESTarData = data;
            return this;
        }
    }
}