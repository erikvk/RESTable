using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.View
{
    partial class Item : RESTarView<object>
    {
        protected override string HtmlMatcher => $"{Resource.Name}-item.html";
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

        private const string regex = @"\@RESTar\((?<content>[^\(\)]*)\)";

        public void Handle(Input.Save action)
        {
            IDictionary<string, JToken> entityJson = JObject.Parse(Entity.ToJson());
            var changes = entityJson.Except(Original, Comparer).ToList();
            changes.ForEach(c => Original[c.Key] = c.Value);
            var json = JsonConvert.SerializeObject(Original).Replace(@"$"":", @""":");
            json = Regex.Replace(json, regex, "${content}");
            if (IsTemplate) POST(json);
            else PATCH(json);
            RedirectUrl = ResourcePath;
        }

        public void Handle(Input.Close action)
        {
            RedirectUrl = Resource.Singleton
                ? $"/{Application.Current.Name}"
                : ResourcePath;
        }

        public void Handle(Input.AddElementTo action)
        {
            try
            {
                var array = (Arr<Json>) action.Value
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(Entity, (json, key) =>
                        int.TryParse(key, out int index)
                            ? (Json) json[index]
                            : (Json) json[key]);
                array.Add();
            }
            catch
            {
                throw new Exception($"Could not add element to '{action.Value}'. Not an array.");
            }
        }

        internal override RESTarView<object> Populate(Request request, object restarData)
        {
            IDictionary<string, object> original;
            var json = "";

            var template = request.Resource.MakeViewModelTemplate();
            var jsonTemplate = template.SerializeVmJsonTemplate();

            if (restarData == null)
            {
                IsTemplate = true;
                original = template;
                base.Populate(request, original);
            }
            else
            {
                base.Populate(request, restarData);
                if (restarData is DDictionary dict)
                {
                    original = dict.ToDictionary(pair => pair.Key + "$", pair => pair.Value);
                    json = original.SerializeDynamicResourceToViewModel();
                }
                else
                {
                    json = restarData.SerializeStaticResourceToViewModel();
                    original = json.Deserialize<IDictionary<string, dynamic>>();
                }
            }

            Entity = new Json {Template = Starcounter.Templates.Template.CreateFromJson(jsonTemplate)};
            if (!IsTemplate)
                Entity.PopulateFromJson(json);
            Original = new JObject();
            original.ForEach(member =>
            {
                if (member.Key == "ObjectID" || member.Key == "ObjectNo") return;
                var okvp = new KeyValuePair<string, JToken>(member.Key, JToken.FromObject(member.Value));
                Original.Add(okvp);
            });
            return this;
        }

        private static readonly MemberComparer Comparer = new MemberComparer();

        private class MemberComparer : IEqualityComparer<KeyValuePair<string, JToken>>
        {
            private readonly JTokenEqualityComparer comparer = new JTokenEqualityComparer();

            public bool Equals(KeyValuePair<string, JToken> x, KeyValuePair<string, JToken> y)
            {
                return x.Key == y.Key && JToken.DeepEquals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<string, JToken> obj)
            {
                return $"{obj.Key}_{comparer.GetHashCode(obj.Value)}".GetHashCode();
            }
        }
    }
}