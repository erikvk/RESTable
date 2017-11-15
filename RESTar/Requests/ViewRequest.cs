using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Serialization;
using RESTar.View;
using Starcounter;
using static RESTar.Methods;
using static RESTar.Internal.ErrorCodes;
using static RESTar.View.MessageTypes;
using static Starcounter.Templates.Template;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class ViewRequest<T> : IRequest<T>, IViewRequest where T : class
    {
        public Origin Origin { get; }
        public IResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        public Stream Body { get; private set; }
        Methods IRequest.Method => GET;
        IResource IRequest.Resource => Resource;
        public Selector<T> View { get; }
        internal Request ScRequest { get; }
        public bool Home => MetaConditions.Empty && Conditions == null;
        internal bool IsTemplate { get; set; }
        internal bool CanInsert { get; set; }
        internal RESTarDataView DataView { get; set; }
        internal IList<T> Entities { get; set; }
        internal T Entity { get; set; }
        internal Json GetView() => DataView.MakeCurrentView();
        private const string MacroRegex = @"\@RESTar\((?<content>[^\(\)]*)\)";

        internal void Evaluate()
        {
            if (MetaConditions.New)
            {
                IsTemplate = true;
                var itemView = new Item {Request = this};
                var itemTemplate = Resource.MakeViewModelTemplate().Serialize();
                itemView.Entity = new Json {Template = CreateFromJson(itemTemplate)};
                DataView = itemView;
                return;
            }
            Entities = Evaluators<T>.SELECT_FILTER(this)?.ToList();
            if (Entities?.Any() != true)
            {
                DataView.SetMessage("No entities found", NoError, warning);
                return;
            }
            if (Resource.IsSingleton || Entities?.Count == 1 && !Home)
            {
                Entity = Entities?[0];
                var itemView = new Item {Request = this};
                var itemTemplate = Resource.MakeViewModelTemplate().Serialize();
                itemView.Entity = new Json {Template = CreateFromJson(itemTemplate)};
                itemView.Entity.PopulateFromJson(Entity.SerializeToViewModel());
                DataView = itemView;
            }
            else
            {
                var listView = new List {Request = this};
                CanInsert = Resource.AvailableMethods.Contains(POST);
                var listTemplate = Resource.MakeViewModelTemplate();
                listView.Entities = new Arr<Json> {Template = CreateFromJson($"[{listTemplate.Serialize()}]")};
                Entities.ForEach(e => listView.Entities.Add().PopulateFromJson(e.SerializeToViewModel()));
                DataView = listView;
            }
        }


        internal ViewRequest(IResource<T> resource, Request scRequest)
        {
            if (resource.IsInternal) throw new ResourceIsInternalException(resource);
            Resource = resource;
            View = resource.Select;
            ScRequest = scRequest;
            Origin = new Origin(scRequest);
            ResponseHeaders = new Dictionary<string, string>();
            MetaConditions = new MetaConditions();
            Conditions = new Condition<T>[0];
        }

        internal void Populate(Args args)
        {
            if (args.HasConditions)
                Conditions = Condition<T>.Parse(args.Conditions, Resource) ?? Conditions;
            if (args.HasMetaConditions)
                MetaConditions = MetaConditions.Parse(args.MetaConditions, Resource, false) ?? MetaConditions;
        }

        public void DeleteFromList(string id)
        {
            Authenticator.CheckUser();
            var list = (List) DataView;
            var conditions = Condition<T>.Parse(id, Resource);
            var item = Entities.Where(conditions).First();
            CheckMethod(DELETE, $"You are not allowed to delete from the '{Resource}' resource");
            Evaluators<T>.View.DELETE(this, item);
            if (string.IsNullOrWhiteSpace(list.RedirectUrl))
                list.RedirectUrl = list.ResourcePath;
        }

        public void SaveItem()
        {
            Authenticator.CheckUser();
            var item = (Item) DataView;
            //var entityJson = item.Entity.ToJson().Replace(@"$"":", @""":");
            //Body = Regex.Replace(entityJson, MacroRegex, "${content}");
            if (IsTemplate)
            {
                CheckMethod(POST, $"You are not allowed to insert into the '{Resource}' resource");
                Evaluators<T>.View.POST(this);
                if (string.IsNullOrWhiteSpace(item.RedirectUrl))
                    item.RedirectUrl = item.ResourcePath;
            }
            CheckMethod(PATCH, $"You are not allowed to update the '{Resource}' resource");
            Evaluators<T>.View.PATCH(this, Entity);
        }

        public void CloseItem()
        {
            var item = (Item) DataView;
            item.RedirectUrl = !string.IsNullOrWhiteSpace(item.RedirectUrl)
                ? item.RedirectUrl
                : Resource.IsSingleton
                    ? $"/{Application.Current.Name}"
                    : item.ResourcePath;
        }

        public void RemoveElementFromArray(string input)
        {
            Authenticator.CheckUser();
            try
            {
                var item = (Item) DataView;
                var parts = input.Split(',');
                var path = parts[0];
                var elementIndex = int.Parse(parts[1]);
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(item.Entity, (json, key) =>
                        int.TryParse(key, out var index)
                            ? (Json) json[index]
                            : (Json) json[key]);
                array.RemoveAt(elementIndex);
            }
            catch (FormatException)
            {
                throw new Exception($"Could not remove element from '{input}'. Invalid syntax.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception($"Could not remove element from '{input}'. Invalid syntax.");
            }
            catch (Exception)
            {
                throw new Exception($"Could not remove element from '{input}'. Not an array.");
            }
        }

        public void AddElementToArray(string input)
        {
            Authenticator.CheckUser();
            try
            {
                var item = (Item) DataView;
                var parts = input.Split(',');
                var path = parts[0];
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(item.Entity, (json, key) =>
                        int.TryParse(key, out var index)
                            ? (Json) json[index]
                            : (Json) json[key]);
                if (parts.Length == 1)
                    array.Add();
                else
                {
                    var value = JToken.Parse(Regex.Replace(parts[1], MacroRegex, "${content}"));
                    switch (value.Type)
                    {
                        case JTokenType.Integer:
                            array.Add().IntegerValue = value.Value<int>();
                            return;
                        case JTokenType.Float:
                            array.Add().DecimalValue = value.Value<decimal>();
                            return;
                        case JTokenType.String:
                            array.Add().StringValue = value.Value<string>();
                            return;
                        case JTokenType.Boolean:
                            array.Add().BoolValue = value.Value<bool>();
                            return;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (JsonReaderException)
            {
                throw new Exception($"Could not add element to '{input}'. Invalid syntax.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception($"Could not add element to '{input}'. Invalid syntax.");
            }
            catch (Exception)
            {
                throw new Exception($"Could not add element to '{input}'. Not an array.");
            }
        }

        private void CheckMethod(Methods method, string errorMessage)
        {
            if (!Authenticator.MethodCheck(method, Resource, AuthToken))
                throw new RESTarException(NotAuthorized, errorMessage);
        }
    }
}