using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests;

[RESTable(Method.GET, Method.PATCH)]
public class GetApiPatchInPlaceTestResource : ISelector<GetApiPatchInPlaceTestResource>, IUpdater<GetApiPatchInPlaceTestResource>
{
    public int Number { get; set; }

    private static List<GetApiPatchInPlaceTestResource> List { get; } = new()
    {
        new GetApiPatchInPlaceTestResource {Number = 1},
        new GetApiPatchInPlaceTestResource {Number = 2},
        new GetApiPatchInPlaceTestResource {Number = 3},
        new GetApiPatchInPlaceTestResource {Number = 4},
        new GetApiPatchInPlaceTestResource {Number = 5}
    };

    public IEnumerable<GetApiPatchInPlaceTestResource> Select(IRequest<GetApiPatchInPlaceTestResource> request)
    {
        return List;
    }

    public IEnumerable<GetApiPatchInPlaceTestResource> Update(IRequest<GetApiPatchInPlaceTestResource> request)
    {
        return request.GetInputEntities();
    }
}