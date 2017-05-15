using System.Collections.Generic;
using System.Data;

namespace RESTar.View
{
    partial class EntitiesView : RESTarView<IEnumerable<object>>
    {
        protected override string HtmlMatcher => $"${Resource.Name}-list.html";
        public List<object[]> _TableRows { get; private set; }
        protected override string DefaultHtml => Resource.EntitiesViewHtml ?? "entitiesview.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;

        internal override RESTarView<IEnumerable<object>> Populate(IRequest request, IEnumerable<object> data)
        {
            base.Populate(request, data);
            var table = data.MakeTable(Resource);
            foreach (DataColumn column in table.Columns)
                TableHead.Add().StringValue = column.ColumnName;
            var columncount = table.Columns.Count;
            _TableRows = new List<object[]>();
            foreach (DataRow row in table.Rows)
            {
                var outputRow = new object[columncount];
                row.ItemArray.ForEach((item, index) =>
                {
                    var toInclude = item;
                    var _int = item as int?;
                    if (_int.HasValue) toInclude = (long) _int;
                    else if (item is IEnumerable<object>)
                        toInclude = string.Join(", ", (IEnumerable<object>) item);
                    outputRow[index] = toInclude;
                });
                _TableRows.Add(outputRow);
            }
            foreach (var entity in data.MakeViewModelJsonArray())
                Entities.Add(entity);
            return this;
        }
    }
}