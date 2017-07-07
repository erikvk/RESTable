using RESTar.Internal;
using Starcounter;

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    partial class MessageWindow : Json, IRESTarView
    {
        void IRESTarView.SetHtml(string html)
        {
        }

        void IRESTarView.SetResourceName(string resourceName)
        {
        }

        void IRESTarView.SetResourcePath(string resourceName)
        {
        }

        IRequestView IRESTarView.Request { get; }
        IResourceView IRESTarView.Resource { get; }
        string IRESTarView.HtmlMatcher { get; }
        bool IRESTarView.Success { get; }

        /// <summary>
        /// </summary>
        public void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        /// <summary>
        /// </summary>
        public MessageWindow Populate()
        {
            Html = "/message.html";
            return this;
        }
    }
}