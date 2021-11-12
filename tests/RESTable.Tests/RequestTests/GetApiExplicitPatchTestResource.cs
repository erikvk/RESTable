using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests
{
    [RESTable(Method.GET, Method.PATCH)]
    public class GetApiExplicitPatchTestResource : ISelector<GetApiExplicitPatchTestResource>, IUpdater<GetApiExplicitPatchTestResource>
    {
        public int Id { get; }
        public int ActualNumber { get; set; }

        public int Number { get; set; }

        public GetApiExplicitPatchTestResource(int id)
        {
            Id = id;
            ActualNumber = id;
            Number = id;
        }

        private static Dictionary<int, GetApiExplicitPatchTestResource> Data { get; } = new()
        {
            [0] = new GetApiExplicitPatchTestResource(0),
            [1] = new GetApiExplicitPatchTestResource(1),
            [2] = new GetApiExplicitPatchTestResource(2),
            [3] = new GetApiExplicitPatchTestResource(3),
            [4] = new GetApiExplicitPatchTestResource(4)
        };

        public IEnumerable<GetApiExplicitPatchTestResource> Select(IRequest<GetApiExplicitPatchTestResource> request)
        {
            return Data.Values;
        }

        public IEnumerable<GetApiExplicitPatchTestResource> Update(IRequest<GetApiExplicitPatchTestResource> request)
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
    }
}