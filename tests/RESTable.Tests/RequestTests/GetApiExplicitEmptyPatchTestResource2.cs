using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests;

[RESTable]
public class GetApiExplicitEmptyPatchTestResource2 : ISelector<GetApiExplicitEmptyPatchTestResource2>, IInserter<GetApiExplicitEmptyPatchTestResource2>,
    IUpdater<GetApiExplicitEmptyPatchTestResource2>, IDeleter<GetApiExplicitEmptyPatchTestResource2>
{
    public GetApiExplicitEmptyPatchTestResource2(int id)
    {
        Id = id;
        ActualNumber = id;
        Number = id;
    }

    public int Id { get; }
    public int ActualNumber { get; set; }

    public int Number { get; set; }

    public static Dictionary<int, GetApiExplicitEmptyPatchTestResource2> Data { get; } = new();

    public int Delete(IRequest<GetApiExplicitEmptyPatchTestResource2> request)
    {
        var i = 0;
        foreach (var entity in request.GetInputEntities())
            if (Data.Remove(entity.Id))
                i += 1;
        return i;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource2> Insert(IRequest<GetApiExplicitEmptyPatchTestResource2> request)
    {
        foreach (var entity in request.GetInputEntities())
        {
            Data.Add(entity.Id, entity);
            yield return entity;
        }
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource2> Select(IRequest<GetApiExplicitEmptyPatchTestResource2> request)
    {
        return Data.Values;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource2> Update(IRequest<GetApiExplicitEmptyPatchTestResource2> request)
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