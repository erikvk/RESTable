using RESTar.Internal;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    partial class MessageWindow : RESTarView
    {
        internal override void SetHtml(string html)
        {
        }

        internal override void SetResourceName(string resourceName)
        {
        }

        internal override void SetResourcePath(string resourceName)
        {
        }

        internal override IRequest Request { get; set; }
        internal override IResource Resource { get; set; }
        internal override string HtmlSuffix { get; }
        internal override bool Success { get; set; }

        /// <summary>
        /// </summary>
        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        /// <summary>
        /// </summary>
        internal MessageWindow Populate()
        {
            Html = "/message.html";
            return this;
        }
    }
}