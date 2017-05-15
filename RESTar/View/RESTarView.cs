using System.IO;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar.View
{
    public abstract class RESTarView<TData> : Json
    {
        internal IRequest Request { get; private set; }
        internal IResource Resource => Request.Resource;
        protected abstract string HtmlMatcher { get; }
        protected abstract string DefaultHtml { get; }
        protected abstract void SetHtml(string html);
        protected abstract void SetResourceName(string resourceName);
        protected abstract void SetResourcePath(string resourceName);
        protected TData RESTarData { get; private set; }

        internal virtual RESTarView<TData> Populate(IRequest request, TData data)
        {
            Request = request;
            RESTarData = data;
            SetResourceName(Resource.Alias ?? Resource.Name);
            SetResourcePath($"{Settings._ViewUri}/{Resource.Alias ?? Resource.Name}");
            var wd = Application.Current.WorkingDirectory;
            var exists = File.Exists($"{wd}/wwwroot/{HtmlMatcher}");
            if (exists)
                SetHtml($"/{HtmlMatcher}");
            else SetHtml($"/{DefaultHtml}");
            return this;
        }
    }
}