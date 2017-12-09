using RESTar.Internal;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class List : RESTarDataView
    {
        protected override string HtmlSuffix => "-list.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;
        protected override void SetPager(string nextPagePath) => NextPagePath = nextPagePath;
        protected override void SetNrOfPages(int nrOfPages) => NrOfPages = nrOfPages;

        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Add action) => RedirectUrl = $"{ResourcePath}//new=true";
        public void Handle(Input.Delete action) => Request.DeleteFromList(action.Value);
    }
}