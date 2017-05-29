using RESTar.Internal;
using Starcounter;

namespace RESTar.View
{
    partial class MessageWindow : Json, IRESTarView
    {
        public void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public MessageWindow Populate()
        {
            Html = "/message.html";
            return this;
        }
    }
}