# Examples

## Custom entity resource

This entity resource is not decorated by any [resource provider attribute](../Resource%20providers). This means that we have to define the operations required by the enabled methods using the [operations interfaces](../#defining-entity-resource-operations). For this example, let's consider an entity resource that works as an interface for `DatabaseRow`.

See [working with requests and conditions](../Working%20with%20requests%20and%20conditions) for more about how to design the individual operations.

```csharp
[RESTable]
public class MyEntityResource : ISelector<MyEntityResource>, IInserter<MyEntityResource>,
    IUpdater<MyEntityResource>, IDeleter<MyEntityResource>
{
    public string TheString { get; set; }
    public int TheInteger { get; set; }
    public DateTime TheDateTime { get; set; }

    /// <summary>
    /// Private properties are not includeded in output and cannot be set in input.
    /// This property is only used internally to determine DB object identity.
    /// </summary>
    private ulong? RowId { get; set; }

    private static MyEntityResource FromDbRow(DatabaseRow dbRow)
    {
        if (dbRow == null) return null;
        return new MyEntityResource
        {
            TheString = dbRow.MyString,
            TheInteger = dbRow.MyInteger,
            TheDateTime = dbRow.MyDateTime,
            RowId = dbRow.GetRowId()
        };
    }

    private static DatabaseRow ToDbRow(MyEntityResource _object)
    {
        if (_object == null) return null;
        var dbRow = _object.RowId is ulong RowId
            ? Db.FromId<DatabaseRow>(RowId)
            : new DatabaseRow();
        dbRow.MyString = _object.TheString;
        dbRow.MyInteger = _object.TheInteger;
        dbRow.MyDateTime = _object.TheDateTime;
        dbRow.MyOtherStarcounterResource = ToDbRow(_object.TheOtherEntityResource);
        return dbRow;
    }

    public IEnumerable<MyEntityResource> Select(IRequest<MyEntityResource> request) => Db
        .SQL<DatabaseRow>($"SELECT t FROM {typeof(DatabaseRow).FullName} t")
        .Select(FromDbRow);

    public IEnumerable<MyEntityResource> Insert(IRequest<MyEntityResource> request) => Db.Transaction(() => request
        .GetInputEntities()
        .Select(ToDbRow)
        .Count());

    public IEnumerable<MyEntityResource> Update(IRequest<MyEntityResource> request) => Db.Transaction(() => request
        .GetInputEntities()
        .Select(ToDbRow));

    public long Delete(IRequest<MyEntityResource> request) => Db.Transaction(() =>
    {
        var i = 0L;
        foreach (var item in request.GetInputEntities())
        {
            item.Delete();
            i += 1;
        }
        return i;
    });
}
```
