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
using RESTar.Results.Fail.Forbidden;
using RESTar.Results.Fail.NotFound;
using RESTar.Serialization;
using RESTar.View;
using Starcounter;
using static RESTar.Methods;
using static RESTar.Internal.ErrorCodes;
using static RESTar.View.MessageTypes;
using static Starcounter.Templates.Template;

namespace RESTar.Requests
{
    internal class ViewRequest<T> : IRequest<T>, IViewRequest where T : class
    {
        public TCPConnection TcpConnection { get; }
        public IEntityResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public string AuthToken { get; internal set; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }
        public IUriParameters UriParameters { get; private set; }
        public Stream Body { get; private set; }
        Methods IRequest.Method => GET;
        IEntityResource IRequest.Resource => Resource;
        public MimeType Accept => MimeType.Default;
        public ITarget<T> Target { get; private set; }
        public bool Home => MetaConditions.Empty && Conditions == null;
        internal bool IsTemplate { get; set; }
        internal bool CanInsert { get; set; }
        internal RESTarDataView DataView { get; set; }
        internal IList<T> Entities { get; set; }
        internal T Entity { get; set; }
        internal Json GetView() => DataView.MakeCurrentView();
        public T1 BodyObject<T1>() where T1 : class => Body?.Deserialize<T1>();
        public Headers Headers { get; }
        public string TraceId { get; }
        
        internal ViewRequest(IEntityResource<T> resource, TCPConnection tcpConnection)
        {
            if (resource.IsInternal) throw new ResourceIsInternal(resource);

            TraceId = tcpConnection.TraceId;
            TcpConnection = tcpConnection;

            Resource = resource;
            Target = resource;
            Headers = new Headers();
            ResponseHeaders = new Headers();
            Cookies = new List<string>();
            MetaConditions = new MetaConditions();
            Conditions = new Condition<T>[0];
        }

        internal void Populate(Arguments arguments)
        {
            if (arguments.Uri.ViewName != null)
            {
                if (!Resource.ViewDictionary.TryGetValue(arguments.Uri.ViewName, out var view))
                    throw new UnknownView(arguments.Uri.ViewName, Resource);
                Target = view;
            }

            UriParameters = arguments.Uri;
            arguments.CustomHeaders.ForEach(Headers.Put);
            Conditions = Condition<T>.Parse(arguments.Uri.Conditions, Resource) ?? Conditions;
            MetaConditions = MetaConditions.Parse(arguments.Uri.MetaConditions, Resource, false) ?? MetaConditions;
        }

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

            var domain = Operations<T>.SELECT_VIEW(this)?.ToList();
            Entities = domain?.Filter(MetaConditions.Offset).Filter(MetaConditions.Limit).ToList();
            if (Entities?.Any() != true || domain == null)
            {
                DataView?.SetMessage("No entities found", NoError, warning);
                return;
            }

            if (Resource.IsSingleton || Entities.Count == 1 && !Home)
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

            // TODO: Add pager here
        }

        public void DeleteFromList(string id)
        {
            Authenticator.CheckUser();
            var list = (List) DataView;
            var conditions = Condition<T>.Parse(id, Resource);
            var item = Entities.Where(conditions).First();
            CheckMethod(DELETE, $"You are not allowed to delete from the '{Resource}' resource");
            Operations<T>.View.DELETE(this, item);
            if (string.IsNullOrWhiteSpace(list.RedirectUrl))
                list.RedirectUrl = list.ResourcePath;
        }

        public void SaveItem()
        {
            Authenticator.CheckUser();
            var item = (Item) DataView;
            var entityJson = item.Entity.ToJson().Replace(@"$"":", @""":");
            Body = new MemoryStream(Regex.Replace(entityJson, RegEx.ViewMacro, "${content}").ToBytes());
            if (IsTemplate)
            {
                CheckMethod(POST, $"You are not allowed to insert into the '{Resource}' resource");
                Operations<T>.View.POST(this);
                if (string.IsNullOrWhiteSpace(item.RedirectUrl))
                    item.RedirectUrl = item.ResourcePath;
            }

            CheckMethod(PATCH, $"You are not allowed to update the '{Resource}' resource");
            Operations<T>.View.PATCH(this, Entity);
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
                    var value = JToken.Parse(Regex.Replace(parts[1], RegEx.ViewMacro, "${content}"));
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
                throw new NotAllowedViewAction(ErrorCodes.NotAuthorized, errorMessage);
        }
    }
}