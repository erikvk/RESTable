using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starcounter;

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

        public override void SetMessage(string message, MessageType messageType)
        {
            Message = message;
            MessageType = messageType.ToString();
        }

        public void Handle(Input.Save action)
        {
            IDictionary<string, JToken> entityJson = JObject.Parse(Entity.ToJson().Replace(@"$"":", @""":"));
            var membersJsonArray = JArray.Parse(Members.ToJson().Replace(@"$"":", @""":"));
            IDictionary<string, JToken> membersJson = new JObject();
            foreach (var member in membersJsonArray)
                membersJson[member["Name"].ToString()] = member["Value"];
            var mchanges = membersJson.Except(Original, Comparer).ToList();
            var echanges = entityJson.Except(Original, Comparer).ToList();
            mchanges.ForEach(c => Original[c.Key] = c.Value);
            echanges.ForEach(c => Original[c.Key] = c.Value);

            var json = JsonConvert.SerializeObject(Original);
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

        internal override RESTarView<object> Populate(IRequest request, object restarData)
        {
            if (restarData == null)
            {
                IsTemplate = true;
                restarData = request.Resource.MakeTemplate();
            }
            base.Populate(request, restarData);
            var original = restarData.MakeDictionary();
            Entity = original.ToJson(JsonSerializer.VmSerializerOptions);
            var properties = request.Resource.TargetType.GetPropertyList();
            Original = new JObject();
            original.ForEach(member =>
            {
                if (member.Key == "ObjectID" || member.Key == "ObjectNo") return;
                var okvp = new KeyValuePair<string, JToken>(member.Key, JToken.FromObject(member.Value ?? ""));
                Original.Add(okvp);
                var json = new Dictionary<string, object>
                {
                    ["Name$"] = member.Key,
                    ["Value$"] = member.Value ?? "",
                    ["Type"] = properties.First(p => p.RESTarMemberName() == member.Key).PropertyType.GetJsType()
                }.SerializeDyn();
                Members.Add(new Json(json));
            });
            properties
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