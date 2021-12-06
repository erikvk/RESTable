using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.RequestTests;

[RESTable(Method.GET)]
public class GetApiTestResource : ISelector<GetApiTestResource>
{
    public int Number { get; set; }

    public IEnumerable<GetApiTestResource> Select(IRequest<GetApiTestResource> request)
    {
        yield return new GetApiTestResource {Number = 0};
        yield return new GetApiTestResource {Number = 1};
        yield return new GetApiTestResource {Number = 2};
        yield return new GetApiTestResource {Number = 3};
        yield return new GetApiTestResource {Number = 4};
        yield return new GetApiTestResource {Number = 5};
        yield return new GetApiTestResource {Number = 6};
        yield return new GetApiTestResource {Number = 7};
        yield return new GetApiTestResource {Number = 8};
        yield return new GetApiTestResource {Number = 9};
        yield return new GetApiTestResource {Number = 10};
        yield return new GetApiTestResource {Number = 11};
        yield return new GetApiTestResource {Number = 12};
        yield return new GetApiTestResource {Number = 13};
        yield return new GetApiTestResource {Number = 14};
        yield return new GetApiTestResource {Number = 15};
        yield return new GetApiTestResource {Number = 16};
        yield return new GetApiTestResource {Number = 17};
        yield return new GetApiTestResource {Number = 18};
        yield return new GetApiTestResource {Number = 19};
        yield return new GetApiTestResource {Number = 20};
        yield return new GetApiTestResource {Number = 21};
        yield return new GetApiTestResource {Number = 22};
        yield return new GetApiTestResource {Number = 23};
        yield return new GetApiTestResource {Number = 24};
        yield return new GetApiTestResource {Number = 25};
        yield return new GetApiTestResource {Number = 26};
        yield return new GetApiTestResource {Number = 27};
        yield return new GetApiTestResource {Number = 28};
        yield return new GetApiTestResource {Number = 29};
        yield return new GetApiTestResource {Number = 30};
        yield return new GetApiTestResource {Number = 31};
        yield return new GetApiTestResource {Number = 32};
        yield return new GetApiTestResource {Number = 33};
        yield return new GetApiTestResource {Number = 34};
        yield return new GetApiTestResource {Number = 35};
        yield return new GetApiTestResource {Number = 36};
        yield return new GetApiTestResource {Number = 37};
        yield return new GetApiTestResource {Number = 38};
        yield return new GetApiTestResource {Number = 39};
        yield return new GetApiTestResource {Number = 40};
        yield return new GetApiTestResource {Number = 41};
        yield return new GetApiTestResource {Number = 42};
        yield return new GetApiTestResource {Number = 43};
        yield return new GetApiTestResource {Number = 44};
        yield return new GetApiTestResource {Number = 45};
        yield return new GetApiTestResource {Number = 46};
        yield return new GetApiTestResource {Number = 47};
        yield return new GetApiTestResource {Number = 48};
        yield return new GetApiTestResource {Number = 49};
        yield return new GetApiTestResource {Number = 50};
        yield return new GetApiTestResource {Number = 51};
        yield return new GetApiTestResource {Number = 52};
        yield return new GetApiTestResource {Number = 53};
        yield return new GetApiTestResource {Number = 54};
        yield return new GetApiTestResource {Number = 55};
        yield return new GetApiTestResource {Number = 56};
        yield return new GetApiTestResource {Number = 57};
        yield return new GetApiTestResource {Number = 58};
        yield return new GetApiTestResource {Number = 59};
    }
}