using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.View;
using Starcounter;
using static RESTar.RESTarMethods;
using Starcounter.Templates;
using static RESTar.Internal.ErrorCodes;
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
        public string Body { get; private set; }
        RESTarMethods IRequest.Method { get; }
        IResource IRequest.Resource => Resource;
        internal Request ScRequest { get; }
        public bool Home => MetaConditions.Empty && Conditions == null;
        internal bool IsTemplate { get; set; }
        internal bool CanInsert { get; set; }

        internal RESTarDataView View { get; set; }
        internal IEnumerable<T> Entities { get; set; }
        internal T Entity { get; set; }
        internal Json GetView() => View.MakeCurrentView();
        private const string MacroRegex = @"\@RESTar\((?<content>[^\(\)]*)\)";

        internal void Evaluate()
        {
            if (MetaConditions.New)
            {
                IsTemplate = true;
                View = new Item();
            }
            Entities = Evaluators<T>.Operations.StatSELECT(this);
            if (IsSingular(Entities, out var item))
            {
                Entity = item;
                var itemView = new Item();
                var itemTemplate = Resource.MakeViewModelTemplate().Serialize();
                itemView.Entity = new Json {Template = Template.CreateFromJson(itemTemplate)};
                itemView.Entity.PopulateFromJson(Entity.SerializeToViewModel());
                View = itemView;
            }
            var listView = new List();
            CanInsert = Resource.AvailableMethods.Contains(POST);
            var listTemplate = Resource.MakeViewModelTemplate();
            listView.Entities = new Arr<Json> {Template = Template.CreateFromJson($"[{listTemplate.Serialize()}]")};
            Entities.ForEach(e => listView.Entities.Add().PopulateFromJson(e.SerializeToViewModel()));
            View = listView;
            View.Request = this;
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
            //if (IsInternal) return;
            //AuthTokens.TryRemove(AuthToken, out var _);
        }

        public void DeleteFromList(string id)
        {
            var list = (List) View;
            var conditions = Conditions.Parse(id, Resource);
            var item = Entities.Filter(conditions).First();
            CheckMethod(DELETE, $"You are not allowed to delete from the '{Resource}' resource");
            Evaluators<T>.View.DELETE(this, item);
            if (string.IsNullOrWhiteSpace(list.RedirectUrl))
                list.RedirectUrl = list.ResourcePath;
        }

        public void SaveItem()
        {
            Authenticator.UserCheck();
            var item = (Item) View;
            var entityJson = item.Entity.ToJson().Replace(@"$"":", @""":");
            Body = Regex.Replace(entityJson, MacroRegex, "${content}");
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
            var item = (Item) View;
            item.RedirectUrl = !string.IsNullOrWhiteSpace(item.RedirectUrl)
                ? item.RedirectUrl
                : Resource.IsSingleton
                    ? $"/{Application.Current.Name}"
                    : item.ResourcePath;
        }

        public void RemoveElementFromArray(string input)
        {
            Authenticator.UserCheck();
            try
            {
                var item = (Item) View;
                var parts = input.Split(',');
                var path = parts[0];
                var elementIndex = int.Parse(parts[1]);
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(item.Entity, (json, key) =>
                        int.TryParse(key, out int index)
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
            try
            {
                var item = (Item) View;
                var parts = input.Split(',');
                var path = parts[0];
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(item.Entity, (json, key) =>
                        int.TryParse(key, out int index)
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

        private void CheckMethod(RESTarMethods method, string errorMessage)
        {
            if (!Authenticator.MethodCheck(method, Resource, AuthToken))
                throw new RESTarException(NotAuthorized, errorMessage);
        }
    }
}