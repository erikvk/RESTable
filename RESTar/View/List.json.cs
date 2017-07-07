using System.Collections.Generic;
using System.IO;
using System.Linq;
using RESTar.Internal;
using Starcounter;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class List : Json, IRESTarView
    {
        public IEnumerable<object> RESTarData;
        public string HtmlMatcher => $"{Resource.Name}-list.html";
        public void SetHtml(string html) => Html = html;
        public void SetResourceName(string resourceName) => ResourceName = resourceName;
        public void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;
        public IRequestView Request { get; set; }
        public IResourceView Resource { get; private set; }
        public bool Success { get; }

        public void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
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
            //DELETE(item);
            RedirectUrl = !string.IsNullOrWhiteSpace(RedirectUrl) ? RedirectUrl : ResourcePath;
        }

        internal List Populate(IRequestView request, IEnumerable<object> data)
        {
            Request = request;
            Resource = request.Resource;
            SetResourceName(Resource.Alias ?? Resource.Name);
            SetResourcePath($"/{Application.Current.Name}/{Resource.Alias ?? Resource.Name}");
            var wd = Application.Current.WorkingDirectory;
            var exists = File.Exists($"{wd}/wwwroot/resources/{HtmlMatcher}");
            if (!exists) exists = File.Exists($"{wd}/../wwwroot/resources/{HtmlMatcher}");
            if (!exists) throw new NoHtmlException(Resource, HtmlMatcher);
            SetHtml($"/resources/{HtmlMatcher}");
            if (data == null)
                SetMessage("No entities found maching query", ErrorCodes.NoError, View.MessageType.info);
            RESTarData = data;

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