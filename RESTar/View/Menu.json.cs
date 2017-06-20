using System.Linq;
using Starcounter;
using RESTar.Internal;

namespace RESTar.View
{
    partial class Menu : Json, IRESTarView
    {
        public void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public Menu Populate()
        {
            Html = "/menu.html";
            MetaResourcePath = $"/{Application.Current.Name}/{typeof(Resource).FullName}";
            RESTarConfig
                .Resources
                .Where(r => r.IsViewable)
                .Select(r => new
                {
                    Name = r.AliasOrName,
                    Url = $"/{Application.Current.Name}/{r.AliasOrName}"
                }.Serialize())
                .ForEach(str => Resources.Add(new Json(str)));
            return this;
        }
    }
}