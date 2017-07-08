using RESTar.Internal;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class List : RESTarView
    {
        internal override string HtmlSuffix => "-list.html";
        internal override void SetHtml(string html) => Html = html;
        internal override void SetResourceName(string resourceName) => ResourceName = resourceName;
        internal override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;
        internal override IViewRequest Request { get; set; }

        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Add unused) => RedirectUrl = $"{ResourcePath}//new=true";

        public void Handle(Input.Delete action)
        {
            Request.DeleteFromList(action.Value);
            RedirectUrl = !string.IsNullOrWhiteSpace(RedirectUrl) ? RedirectUrl : ResourcePath;
        }
    }
}