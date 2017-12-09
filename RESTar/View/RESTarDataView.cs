using System.IO;
using RESTar.Internal;
using RESTar.Requests;
using Starcounter;

#pragma warning disable 1591

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    public enum MessageTypes
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

    public abstract class RESTarView : Json
    {
        internal abstract void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType);
    }

    public abstract class RESTarDataView : RESTarView
    {
        protected abstract void SetResourceName(string resourceName);
        protected abstract void SetResourcePath(string resourceName);
        protected abstract void SetPager(string nextPagePath);
        protected abstract void SetNrOfPages(int nrOfPages);
        protected abstract void SetHtml(string html);
        protected abstract string HtmlSuffix { get; }
        private IViewRequest _request;

        internal IViewRequest Request
        {
            get => _request;
            set
            {
                SetResourceName(value.Resource.Name);
                SetResourcePath($"/{Application.Current.Name}/{value.Resource.Name}");
                var wd = Application.Current.WorkingDirectory;
                var html = $"{value.Resource.Name}{HtmlSuffix}";
                var exists = File.Exists($"{wd}/wwwroot/resources/{html}");
                if (!exists) exists = File.Exists($"{wd}/../wwwroot/resources/{html}");
                if (!exists) throw new NoHtmlException(value.Resource, html);
                SetHtml($"/resources/{html}");
                _request = value;
            }
        }
    }
}