using System.Collections.Generic;
using System.Data;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar.View
{
    partial class EntitiesView : Json, IRESTarView, IBound<IEnumerable<object>>
    {
        public IResource Resource { get; set; }
        public IRequest Request { get; set; }

        public List<object[]> ResultRows { get; set; }

        internal static EntitiesView Make(IRequest request, IEnumerable<object> data)
        {
            var view = new EntitiesView
            {
                Request = request,
                Resource = request.Resource,
                ResourceName = request.Resource.Name
            };
            view.Html = request.Resource.EntitiesViewHtmlPath ?? view.Html;
            var table = data.MakeTable(request.Resource);
            foreach (DataColumn column in table.Columns)
                view.TableHead.Add().StringValue = column.ColumnName;
            var columncount = table.Columns.Count;
            view.ResultRows = new List<object[]>();
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
                view.ResultRows.Add(outputRow);
            }
            foreach (var entity in data.ToEntities())
            {
                view.Entities.Add(entity);
            }
            return view;
        }
    }
}