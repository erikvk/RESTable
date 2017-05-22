using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.View
{
    partial class Item : RESTarView<object>
    {
        protected override string HtmlMatcher => $"/resources/{Resource.Name}-item.html";
        protected override string DefaultHtml => "/defaults/item.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;

        private bool IsTemplate;
        private IDictionary<string, JToken> Original;

        public override void SetMessage(string message, ErrorCode errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Save action)
        {
            IDictionary<string, JToken> entityJson = JObject.Parse(Entity.ToJson());
            var membersJsonArray = JArray.Parse(Members.ToJson());
            IDictionary<string, JToken> membersJson = new JObject();
            foreach (var member in membersJsonArray)
                membersJson[$"{member["Name$"]}$"] = member["Value$"];
            var mchanges = membersJson.Except(Original, Comparer).ToList();
            var echanges = entityJson.Except(Original, Comparer).ToList();
            mchanges.ForEach(c => Original[c.Key] = c.Value);
            echanges.ForEach(c => Original[c.Key] = c.Value);
            var json = JsonConvert.SerializeObject(Original).Replace(@"$"":", @""":");
            if (IsTemplate) POST(json);
            else PATCH(json);
            RedirectUrl = ResourcePath;
        }

        public void Handle(Input.Close action)
        {
            RedirectUrl = ResourcePath;
        }

        public void Handle(Input.AddMember action)
        {
        }

        internal override RESTarView<object> Populate(Request request, object restarData)
        {
            string json;
            IDictionary<string, object> original;

            if (restarData == null)
            {
                IsTemplate = true;
                original = request.Resource.MakeViewModelTemplate();
                json = original.SerializeDyn();
                base.Populate(request, original);
            }
            else
            {
                base.Populate(request, restarData);
                if (restarData is DDictionary)
                {
                    original = ((DDictionary) restarData).ToDictionary(pair => pair.Key + "$", pair => pair.Value);
                    json = original.SerializeDyn();
                }
                else
                {
                    json = restarData.SerializeToViewModel();
                    original = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }
            }

            Entity = new Json(json);
            Original = new JObject();
            original.ForEach(member =>
            {
                if (member.Key == "ObjectID$" || member.Key == "ObjectNo$") return;
                var okvp = new KeyValuePair<string, JToken>(member.Key, JToken.FromObject(member.Value));
                Original.Add(okvp);
                var memberJson = new Dictionary<string, object>
                {
                    ["Name$"] = member.Key.TrimEnd('$'),
                    ["Value$"] = member.Value,
                    ["Type"] = member.Value.GetType().GetJsType()
                }.SerializeDyn();
                Members.Add(new Json(memberJson));
            });
            if (restarData is DDictionary)
                return this;
            request.Resource.TargetType.GetPropertyList()
                .Where(p => p.SetMethod?.IsPublic != true)
                .ForEach(p => ReadOnlyMembers.Add().StringValue = p.RESTarMemberName() + "$");
            return this;
        }

        private static readonly MemberComparer Comparer = new MemberComparer();

        private class MemberComparer : IEqualityComparer<KeyValuePair<string, JToken>>
        {
            private readonly JTokenEqualityComparer Comparer = new JTokenEqualityComparer();

            public bool Equals(KeyValuePair<string, JToken> x, KeyValuePair<string, JToken> y) => x.Key == y.Key &&
                                                                                                  JToken.DeepEquals(
                                                                                                      x.Value, y.Value);

            public int GetHashCode(KeyValuePair<string, JToken> obj) => $"{obj.Key}_{Comparer.GetHashCode(obj.Value)}"
                .GetHashCode();
        }
    }
}