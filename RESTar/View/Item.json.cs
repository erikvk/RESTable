using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using Starcounter;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class Item : RESTarView
    {
        internal override IViewRequest Request { get; set; }

        internal override void SetHtml(string html) => Html = html;
        internal override void SetResourceName(string resourceName) => ResourceName = resourceName;
        internal override void SetResourcePath(string resourceName) => ResourcePath = resourceName;
        internal override string HtmlSuffix => "-item.html";

        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Save action) => RedirectUrl = Request.SaveItem();

        /// <summary>
        /// </summary>
        public void Handle(Input.Close action)
        {
            RedirectUrl = !string.IsNullOrWhiteSpace(RedirectUrl)
                ? RedirectUrl
                : Resource.IsSingleton
                    ? $"/{Application.Current.Name}"
                    : ResourcePath;
        }

        /// <summary>
        /// </summary>
        public void Handle(Input.RemoveElementFrom action)
        {
            try
            {
                var parts = action.Value.Split(',');
                var path = parts[0];
                var elementIndex = int.Parse(parts[1]);
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(Entity, (json, key) =>
                        int.TryParse(key, out int index)
                            ? (Json) json[index]
                            : (Json) json[key]);
                array.RemoveAt(elementIndex);
                action.Cancel();
            }
            catch (FormatException)
            {
                throw new Exception($"Could not remove element from '{action.Value}'. Invalid syntax.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception($"Could not remove element from '{action.Value}'. Invalid syntax.");
            }
            catch (Exception)
            {
                throw new Exception($"Could not remove element from '{action.Value}'. Not an array.");
            }
        }

        /// <summary>
        /// </summary>
        public void Handle(Input.AddElementTo action)
        {
            try
            {
                var parts = action.Value.Split(',');
                var path = parts[0];
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(Entity, (json, key) =>
                        int.TryParse(key, out int index)
                            ? (Json) json[index]
                            : (Json) json[key]);
                if (parts.Length == 1)
                    array.Add();
                else
                {
                    var value = JToken.Parse(regex.Replace(parts[1], "${content}"));
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
                action.Cancel();
            }
            catch (JsonReaderException)
            {
                throw new Exception($"Could not add element to '{action.Value}'. Invalid syntax.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception($"Could not add element to '{action.Value}'. Invalid syntax.");
            }
            catch (Exception)
            {
                throw new Exception($"Could not add element to '{action.Value}'. Not an array.");
            }
        }

        private void PopulateInternal(IRequest request, object restarData)
        {
            if (restarData == null)
                SetMessage("No entities found maching query", ErrorCodes.NoError, MessageTypes.info);
        }
    }
}