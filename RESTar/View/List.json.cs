using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
using RESTar.Internal;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.View
{
    partial class List : RESTarView<IEnumerable<object>>
    {
        protected override string HtmlMatcher => $"{Resource.Name}-list.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;

        public override void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Add action)
        {
            RedirectUrl = $"{ResourcePath}//new=true";
        }

        public void Handle(Input.Delete action)
        {
            var id = action.Value;
            var conditions = Requests.Conditions.Parse(id, Resource);
            var item = RESTarData.Filter(conditions).First();
            DELETE(item);
            RedirectUrl = $"{ResourcePath}";
        }

        internal override RESTarView<IEnumerable<object>> Populate(Request request, IEnumerable<object> data)
        {
            base.Populate(request, data);
            CanInsert = Resource.AvailableMethods.Contains(RESTarMethods.POST);
            if (data?.Any() != true) return this;
            var template = request.Resource.MakeViewModelTemplate();
            var jsonTemplate = $"[{template.SerializeVmJsonTemplate()}]";
            Entities = new Arr<Json> {Template = Starcounter.Templates.Template.CreateFromJson(jsonTemplate)};
            data.ForEach(e => Entities.Add().PopulateFromJson(e.SerializeStaticResourceToViewModel()));
            return this;
        }
    }
}