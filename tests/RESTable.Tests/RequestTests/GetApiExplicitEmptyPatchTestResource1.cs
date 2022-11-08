using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests;

[RESTable]
public class GetApiExplicitEmptyPatchTestResource1 : ISelector<GetApiExplicitEmptyPatchTestResource1>, IInserter<GetApiExplicitEmptyPatchTestResource1>,
    IUpdater<GetApiExplicitEmptyPatchTestResource1>, IDeleter<GetApiExplicitEmptyPatchTestResource1>
{
    public GetApiExplicitEmptyPatchTestResource1(int id)
    {
        Id = id;
        ActualNumber = id;
        Number = id;
    }

    public int Id { get; }
    public int ActualNumber { get; set; }

    public int Number { get; set; }

    public static Dictionary<int, GetApiExplicitEmptyPatchTestResource1> Data { get; } = new();

    public int Delete(IRequest<GetApiExplicitEmptyPatchTestResource1> request)
    {
        var i = 0;
        foreach (var entity in request.GetInputEntities())
            if (Data.Remove(entity.Id))
                i += 1;
        return i;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource1> Insert(IRequest<GetApiExplicitEmptyPatchTestResource1> request)
    {
        foreach (var entity in request.GetInputEntities())
        {
            Data.Add(entity.Id, entity);
            yield return entity;
        }
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource1> Select(IRequest<GetApiExplicitEmptyPatchTestResource1> request)
    {
        return Data.Values;
    }

    public IEnumerable<GetApiExplicitEmptyPatchTestResource1> Update(IRequest<GetApiExplicitEmptyPatchTestResource1> request)
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
