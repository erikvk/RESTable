using RESTar.Internal;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class MessageWindow : RESTarView
    {
        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        internal MessageWindow Populate()
        {
            Html = "/message.html";
            return this;
        }
    }
}