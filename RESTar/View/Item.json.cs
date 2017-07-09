using RESTar.Internal;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class Item : RESTarDataView
    {
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourceName) => ResourcePath = resourceName;
        protected override string HtmlSuffix => "-item.html";
        
        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Save action) => Request.SaveItem();
        public void Handle(Input.Close action) => Request.CloseItem();

        public void Handle(Input.RemoveElementFrom action)
        {
            Request.RemoveElementFromArray(action.Value);
            action.Cancel();
        }

        public void Handle(Input.AddElementTo action)
        {
            Request.AddElementToArray(action.Value);
            action.Cancel();
        }
    }
}