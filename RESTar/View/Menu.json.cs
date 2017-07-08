using System.Linq;
using Starcounter;
using RESTar.Internal;
using IResource = RESTar.Internal.IResource;

#pragma warning disable 1591

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    partial class Menu : RESTarView
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