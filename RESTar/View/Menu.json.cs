using RESTar.Internal;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class Menu : RESTarView
    {
        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public Menu Populate()
        {
            return null;
            // Html = "/menu.html";
            // MetaResourcePath = $"/{Application.Current.Name}/{typeof(Resource).FullName}";
            // RESTarConfig
            //     .Resources
            //     .Select(r => new
            //     {
            //         r.Name,
            //         Url = $"/{Application.Current.Name}/{r.Name}"
            //     }.Serialize())
            //     .ForEach(str => Resources.Add(new Json(str)));
            // return this;
        }
    }
}