using RESTar.Internal;
using Starcounter;

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    partial class MessageWindow : Json, IRESTarView
    {
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