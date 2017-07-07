using RESTar.Internal;

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
        void SetHtml(string html);
        void SetResourceName(string resourceName);
        void SetMessage(string message, ErrorCodes errorCode, MessageType messageType);
        void SetResourcePath(string resourceName);
        IRequestView Request { get; }
        IResourceView Resource { get; }
        string HtmlMatcher { get; }
        bool Success { get; }
    }
}