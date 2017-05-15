using System;
using Jil;
using RESTar.Operations;
using Starcounter;

namespace RESTar.View
{
    partial class EntityView : RESTarView<object>
    {
        protected override string HtmlMatcher => $"${Resource.Name}.html";
        protected override string DefaultHtml => Resource.EntityViewHtml ?? "entityview.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;

        public void Handle(Input.Save action)
        {
            var entityJson = Entity.ToJson().Replace(@"$"":", @""":");
            Evaluators.PATCH(RESTarData, entityJson, Request);
        }

        internal override RESTarView<object> Populate(IRequest request, object data)
        {
            base.Populate(request, data);
            Entity = data.MakeDictionary().ToJson(JsonSerializer.VmSerializerOptions);

            var eJson = Entity.ToJson();

            return this;
        }
    }
}