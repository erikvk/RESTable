using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.View
{
    partial class List : RESTarView<IEnumerable<object>>
    {
        public List<object[]> _TableRows { get; private set; }
        protected override string HtmlMatcher => $"/resources/{Resource.Name}-list.html";
        protected override string DefaultHtml => "/defaults/list.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;

        public override void SetMessage(string message, ErrorCode errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public void Handle(Input.Add action)
        {
            RedirectUrl = $"{ResourcePath}//new=true";
        }

        internal override RESTarView<IEnumerable<object>> Populate(Request request, IEnumerable<object> data)
        {
            base.Populate(request, data);
            CanInsert = Resource.AvailableMethods.Contains(RESTarMethods.POST);
            if (data?.Any() != true) return this;
            var uniqueIdentifiers = Resource.GetUniqueIdentifiers();
            uniqueIdentifiers.ForEach(id => UniqueIdentifiers.Add().StringValue = id);
            var propertyNames = new HashSet<string>();
            foreach (var item in data)
            {
                string json;
                IDictionary<string, object> original;
                if (item is DDictionary)
                {
                    original = ((DDictionary) item).ToDictionary(pair => pair.Key + "$", pair => pair.Value);
                    json = original.SerializeDyn();
                }
                else
                {
                    json = item.SerializeToViewModel();
                    original = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }
                original.Keys.ForEach(k => propertyNames.Add(k.TrimEnd('$')));
                Entities.Add(new Json(json));
            }
            propertyNames.ForEach(name => TableHead.Add().StringValue = name);

            #region table

//            var table = data.MakeTable(Resource);
//            foreach (DataColumn column in table.Columns)
//                TableHead.Add().StringValue = column.ColumnName;
//              var columncount = table.Columns.Count;
//            _TableRows = new List<object[]>();
//            foreach (DataRow row in table.Rows)
//            {
//                var outputRow = new object[columncount];
//                row.ItemArray.ForEach((item, index) =>
//                {
//                    var toInclude = item;
//                    var _int = item as int?;
//                    if (_int.HasValue) toInclude = (long)_int;
//                    else if (item is IEnumerable<object>)
//                        toInclude = string.Join(", ", (IEnumerable<object>)item);
//                    outputRow[index] = toInclude;
//                });
//                _TableRows.Add(outputRow);
//            }

            #endregion

            return this;
        }
    }
}