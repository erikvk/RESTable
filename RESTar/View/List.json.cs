using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
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

        public override void SetMessage(string message, ErrorCode errorCode, MessageType messageType)
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

            var unique = Resource.GetUniqueIdentifiers();
            unique.ForEach(id => UniqueIdentifiers.Add().StringValue = id);

            var template = request.Resource.MakeViewModelTemplate();
            var jsonTemplate = $"[{template.SerializeVmJsonTemplate()}]";
            Entities = new Arr<Json> { Template = Starcounter.Templates.Template.CreateFromJson(jsonTemplate) };

            var propertyNames = new HashSet<string>();
            foreach (var item in data)
            {
                string json;
                IDictionary<string, object> original;
                if (item is DDictionary dict)
                {
                    original = dict.ToDictionary(pair => pair.Key + "$", pair => pair.Value);
                    json = original.SerializeDyn();
                }
                else
                {
                    json = item.SerializeStaticResourceToViewModel();
                    original = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }
                original.Keys.ForEach(k => propertyNames.Add(k.TrimEnd('$')));
                Entities.Add().PopulateFromJson(json);
            }
            propertyNames.ForEach(name => TableHead.Add().StringValue = name);
            return this;
        }
    }
}