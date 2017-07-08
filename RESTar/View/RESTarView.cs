using RESTar.Internal;
using Starcounter;

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
        internal abstract void SetHtml(string html);
        internal abstract void SetResourceName(string resourceName);
        internal abstract void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType);
        internal abstract void SetResourcePath(string resourceName);
        internal abstract IViewRequest Request { get; set; }
        internal abstract string HtmlSuffix { get; }
    }
}