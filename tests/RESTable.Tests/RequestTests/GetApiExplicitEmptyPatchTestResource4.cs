using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests
{
    [RESTable]
    public class GetApiExplicitEmptyPatchTestResource4 : ISelector<GetApiExplicitEmptyPatchTestResource4>, IInserter<GetApiExplicitEmptyPatchTestResource4>,
        IUpdater<GetApiExplicitEmptyPatchTestResource4>, IDeleter<GetApiExplicitEmptyPatchTestResource4>
    {
        public int Id { get; }
        public int ActualNumber { get; set; }

        public int Number { get; set; }

        public GetApiExplicitEmptyPatchTestResource4(int id)
        {
            Id = id;
            ActualNumber = id;
            Number = id;
        }

        public static Dictionary<int, GetApiExplicitEmptyPatchTestResource4> Data { get; } = new();

        public IEnumerable<GetApiExplicitEmptyPatchTestResource4> Select(IRequest<GetApiExplicitEmptyPatchTestResource4> request)
        {
            return Data.Values;
        }

        public IEnumerable<GetApiExplicitEmptyPatchTestResource4> Insert(IRequest<GetApiExplicitEmptyPatchTestResource4> request)
        {
            foreach (var entity in request.GetInputEntities())
            {
                Data.Add(entity.Id, entity);
                yield return entity;
            }
        }

        public IEnumerable<GetApiExplicitEmptyPatchTestResource4> Update(IRequest<GetApiExplicitEmptyPatchTestResource4> request)
        {
            foreach (var item in request.GetInputEntities())
            {
                if (Data.TryGetValue(item.Id, out var existing))
                {
                    if (existing!.ActualNumber == item.Number)
                    {
                        // No update
                        continue;
                    }
                    existing.ActualNumber = item.Number;
                    yield return existing;
                }
                else
                {
                    // Skip unknowns
                }
            }
        }

        public int Delete(IRequest<GetApiExplicitEmptyPatchTestResource4> request)
        {
            var i = 0;
            foreach (var entity in request.GetInputEntities())
            {
                if (Data.Remove(entity.Id))
                    i += 1;
            }
            return i;
        }
    }
}