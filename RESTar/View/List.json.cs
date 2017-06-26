using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using Starcounter;

namespace RESTar.View
{
    /// <summary>
    /// </summary>
    partial class List : RESTarView<IEnumerable<object>>
    {
        /// <summary>
        /// </summary>
        protected override string HtmlMatcher => $"{Resource.Name}-list.html";

        /// <summary>
        /// </summary>
        protected override void SetHtml(string html) => Html = html;

        /// <summary>
        /// </summary>
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;

        /// <summary>
        /// </summary>
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;

        /// <summary>
        /// </summary>
        public override void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        /// <summary>
        /// </summary>
        public void Handle(Input.Add action)
        {
            RedirectUrl = $"{ResourcePath}//new=true";
        }

        /// <summary>
        /// </summary>
        public void Handle(Input.Delete action)
        {
            var id = action.Value;
            var conditions = Conditions.Parse(id, Resource);
            var item = RESTarData.Filter(conditions).First();
            DELETE(item);
            RedirectUrl = !string.IsNullOrWhiteSpace(RedirectUrl) ? RedirectUrl : ResourcePath;
        }

        internal override RESTarView<IEnumerable<object>> Populate(Requests.HttpRequest request,
            IEnumerable<object> data)
        {
            base.Populate(request, data);
            CanInsert = Resource.AvailableMethods.Contains(RESTarMethods.POST);
            if (data?.Any() != true) return this;
            var template = request.Resource.MakeViewModelTemplate();
            var jsonTemplate = $"[{template.Serialize()}]";
            Entities = new Arr<Json> {Template = Starcounter.Templates.Template.CreateFromJson(jsonTemplate)};
            data.ForEach(e => Entities.Add().PopulateFromJson(e.SerializeToViewModel()));
            return this;
        }
    }
}