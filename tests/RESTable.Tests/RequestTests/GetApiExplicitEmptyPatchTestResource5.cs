using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests;

[RESTable]
public class GetApiExplicitEmptyPatchTestResource5 : ISelector<GetApiExplicitEmptyPatchTestResource5>, IInserter<GetApiExplicitEmptyPatchTestResource5>,
    IUpdater<GetApiExplicitEmptyPatchTestResource5>, IDeleter<GetApiExplicitEmptyPatchTestResource5>
{
    public GetApiExplicitEmptyPatchTestResource5(int id)
    {
        Id = id;
        ActualNumber = id;
        Number = id;
    }

    public int Id { get; }
    public int ActualNumber { get; set; }

    public int Number { get; set; }

    public static Dictionary<int, GetApiExplicitEmptyPatchTestResource5> Data { get; } = new();

    public int Delete(IRequest<GetApiExplicitEmptyPatchTestResource5> request)
    {
        var i = 0;
        foreach (var entity in request.GetInputEntities())
            if (Data.Remove(entity.Id))
                i += 1;
        return i;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource5> Insert(IRequest<GetApiExplicitEmptyPatchTestResource5> request)
    {
        foreach (var entity in request.GetInputEntities())
        {
            Data.Add(entity.Id, entity);
            entity.ActualNumber = entity.Number;
            yield return entity;
        }
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource5> Select(IRequest<GetApiExplicitEmptyPatchTestResource5> request)
    {
        return Data.Values;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource5> Update(IRequest<GetApiExplicitEmptyPatchTestResource5> request)
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