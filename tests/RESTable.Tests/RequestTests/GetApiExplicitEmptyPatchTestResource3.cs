using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests;

[RESTable]
public class GetApiExplicitEmptyPatchTestResource3 : ISelector<GetApiExplicitEmptyPatchTestResource3>, IInserter<GetApiExplicitEmptyPatchTestResource3>,
    IUpdater<GetApiExplicitEmptyPatchTestResource3>, IDeleter<GetApiExplicitEmptyPatchTestResource3>
{
    public GetApiExplicitEmptyPatchTestResource3(int id)
    {
        Id = id;
        ActualNumber = id;
        Number = id;
    }

    public int Id { get; }
    public int ActualNumber { get; set; }

    public int Number { get; set; }

    public static Dictionary<int, GetApiExplicitEmptyPatchTestResource3> Data { get; } = new();

    public int Delete(IRequest<GetApiExplicitEmptyPatchTestResource3> request)
    {
        var i = 0;
        foreach (var entity in request.GetInputEntities())
            if (Data.Remove(entity.Id))
                i += 1;
        return i;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource3> Insert(IRequest<GetApiExplicitEmptyPatchTestResource3> request)
    {
        foreach (var entity in request.GetInputEntities())
        {
            Data.Add(entity.Id, entity);
            yield return entity;
        }
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource3> Select(IRequest<GetApiExplicitEmptyPatchTestResource3> request)
    {
        return Data.Values;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource3> Update(IRequest<GetApiExplicitEmptyPatchTestResource3> request)
    {
        foreach (var item in request.GetInputEntities())
            if (Data.TryGetValue(item.Id, out var existing))
            {
                if (existing.ActualNumber == item.Number)
                    // No update
                    continue;
                existing.ActualNumber = item.Number;
                yield return existing;
            }
    }
}
