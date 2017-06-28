using System.Linq;
using Starcounter;
using RESTar.Internal;

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    partial class Menu : Json, IRESTarView
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
        public Menu Populate()
        {
            Html = "/menu.html";
            MetaResourcePath = $"/{Application.Current.Name}/{typeof(Resource).FullName}";
            RESTarConfig
                .Resources
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