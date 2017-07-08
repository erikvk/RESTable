using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.View;
using Starcounter;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using Starcounter.Templates;
using IResource = RESTar.Internal.IResource;

// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable 1591

namespace RESTar.Requests
{
    internal class ViewRequest<T> : IRequest<T>, IViewRequest where T : class
    {
        public IResource<T> Resource { get; }
        public Conditions Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public string AuthToken { get; internal set; }
        public bool IsInternal { get; }
        public IDictionary<string, string> ResponseHeaders { get; }
        public string Body { get; }
        RESTarMethods IRequest.Method { get; }
        IResource IRequest.Resource => Resource;

        internal Json GetView() => View.MakeCurrentView();
        internal Request ScRequest { get; }
        public bool Home => MetaConditions.Empty && Conditions == null;
        public IEnumerable<T> Data { get; set; }
        private RESTarView View { get; set; }
        private static readonly Regex MacroRegex = new Regex(@"\@RESTar\((?<content>[^\(\)]*)\)");

        internal bool IsTemplate { get; set; }
        internal bool CanInsert { get; set; }

        private RESTarView SetView()
        {
            if (MetaConditions.New)
            {
                IsTemplate = true;
                return new Item();
            }
            var entities = Evaluators<T>.Operations.StatSELECT(this);
            if (IsSingular(entities, out var item))
            {
                var itemView = new Item();
                var itemTemplate = Resource.MakeViewModelTemplate().Serialize();
                itemView.Entity = new Json {Template = Template.CreateFromJson(itemTemplate)};
                itemView.Entity.PopulateFromJson(item.SerializeToViewModel());
                return itemView;
            }
            var listView = new List();
            CanInsert = Resource.AvailableMethods.Contains(POST);
            var listTemplate = Resource.MakeViewModelTemplate();
            listView.Entities = new Arr<Json> {Template = Template.CreateFromJson($"[{listTemplate.Serialize()}]")};
            entities.ForEach(e => listView.Entities.Add().PopulateFromJson(e.SerializeToViewModel()));
            return listView;
        }

        internal void Evaluate()
        {
            View = SetView();
            View.Request = this;
            View.SetResourceName(Resource.Alias ?? Resource.Name);
            View.SetResourcePath($"/{Application.Current.Name}/{Resource.Alias ?? Resource.Name}");
            var wd = Application.Current.WorkingDirectory;
            var html = $"{Resource.Name}{View.HtmlSuffix}";
            var exists = File.Exists($"{wd}/wwwroot/resources/{html}");
            if (!exists) exists = File.Exists($"{wd}/../wwwroot/resources/{html}");
            if (!exists) throw new NoHtmlException(Resource, html);
            View.SetHtml($"/resources/{html}");
        }

        internal bool IsSingular(IEnumerable<T> ienum, out T item)
        {
            item = null;
            if (Resource.IsSingleton || ienum.ExaclyOne() && !Home)
            {
                item = ienum.First();
                return true;
            }
            return false;
        }

        internal ViewRequest(IResource<T> resource, Request scRequest)
        {
            Resource = resource;
            ScRequest = scRequest;
            IsInternal = !ScRequest.IsExternal;
            ResponseHeaders = new Dictionary<string, string>();
            MetaConditions = new MetaConditions();
        }

        internal void Populate(string[] args)
        {
            if (args.Length <= 2) return;
            Conditions = Conditions.Parse(args[2], Resource);
            if (args.Length == 3) return;
            MetaConditions = MetaConditions.Parse(args[3], Resource, parseProcessors: false) ?? MetaConditions;
        }

        public void Dispose()
        {
            // TODO: keep tokens as long as user is logged in
            if (IsInternal) return;
            AuthTokens.TryRemove(AuthToken, out var _);
        }

        public void DeleteFromList(string id)
        {
            var list = (List) View;
            var conditions = Conditions.Parse(id, Resource);
            var item = Data.Filter(conditions).First();
            // DELETE(item);
        }

        public string SaveItem()
        {
            var item = (Item) View;
            var entityJson = item.Entity.ToJson().Replace(@"$"":", @""":");
            var json = MacroRegex.Replace(entityJson, "${content}");
            if (IsTemplate)
                POST(json);
            else PATCH(json);
            if (IsTemplate && Success)
                return !string.IsNullOrWhiteSpace(item.RedirectUrl) ? item.RedirectUrl : item.ResourcePath;
            Success = false;
        }
    }
}