_By Erik von Krusenstierna (erik.von.krusenstierna@mopedo.com)_

# Tutorial

RESTable is a powerful REST API framework for Starcounter applications, that is free to use and easy to set up in new or
existing applications. Using RESTable in your projects will give your applications all sorts of REST super powers, with
minimal effort. This tutorial will give a hands-on introduction to RESTable, and show how to use it in a simple
Starcounter application. The resulting application is available in this repository as
a [Visual Studio solution](RESTableTutorial), so you can download it and try things out for yourself. For more
information
about RESTable, see the [RESTable Specification](https://develop.mopedo.com/RESTable/).

## Getting started

To get started, install RESTable from NuGet, either by browsing for `RESTable` in the **NuGet Package Manager** or by
running the following command in the **Package Manager Console**:

```
Install-Package RESTable
```

All we need to do then, to enable RESTable and set up a REST API for a given application, is to make a call
to `RESTable.RESTableConfig.Init()` somewhere in the application code, preferably where it's called once every time the
app
starts. `Init()` will register the necessary HTTP handlers, collect resources and make them available over a REST API.
Here is a simple RESTable application:

```csharp
namespace RESTableTutorial
{
    using RESTable;
    using Starcounter;
    using static RESTable.Methods;

    public class TutorialApp
    {
        public static void Main()
        {
            RESTableConfig.Init(port: 8282, uri: "/api");
            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
        }
    }
}
```

The application above is not very useful, however, since it doesn't expose any data through the REST API. Let's change
that. RESTable can take any Starcounter database class and make its content available as a web resource in the REST API.
To tell RESTable which classes to expose, we simply decorate their definitions with the `RESTableAttribute` attribute
and
provide the REST methods we would like to enable for the resource in its constructor. Let's add a web resource to our
application:

```csharp
namespace RESTableTutorial
{
    using RESTable;
    using Starcounter;
    using static RESTable.Methods;

    public class TutorialApp
    {
        public static void Main()
        {
            RESTableConfig.Init(port: 8282, uri: "/api");
        }
    }

    [Database, RESTable(GET, POST, PUT, PATCH, DELETE)]
    public class Superhero
    {
        public string Name { get; set; }
        public bool HasSecretIdentity { get; set; }
        public string Gender { get; set; }
        public int? YearIntroduced { get; set; }
        public DateTime InsertedAt { get; }
        public Superhero() => InsertedAt = DateTime.Now;
    }
}
```

When `RESTableConfig.Init()` is called, RESTable will find the `Superhero` database class and register it as available
over
the REST API. This means that REST clients can send `GET`, `POST`, `PUT`, `PATCH` and `DELETE` requests
to `<host>:8282/api/superhero` and interact with its content. To make a different set of methods available for a
resource, we simply include a different set of methods in the `RESTableAttribute` constructor. RESTable can read and
write **JSON** and **Excel**, so the bodies contained within these requests can be of either of these formats. Now let's
make a simple local `POST` request to this API with JSON data (using cURL syntax) (
or [Postman](https://github.com/Mopedo/RESTable.Tutorial/blob/master/RESTableTutorial/Postman_data_post.jpg)):

```
curl 'localhost:8282/api/superhero' -d '[{
    "Name": "Wonder Woman",
    "HasSecretIdentity": false,
    "Gender": "Female",
    "YearIntroduced": 1941
},
{
    "Name": "Superman",
    "HasSecretIdentity": true,
    "Gender": "Male",
    "YearIntroduced": 1938
}]'
```

> RESTable will map properties from JSON to the .NET class automatically. We can configure this mapping by decorating
> properties with the `RESTableMemberAttribute` attribute, but for now – let's keep things simple.

And now, let's retrieve this data using a `GET`
request ([Postman](https://github.com/Mopedo/RESTable.Tutorial/blob/master/RESTableTutorial/Postman_data_get.jpg)):

```
curl 'localhost:8282/api/superhero//limit=2'
Output:
[{
    "Name": "Wonder Woman",
    "HasSecretIdentity": false,
    "Gender": "Female",
    "YearIntroduced": 1941
    "InsertedAt": "2018-08-08T18:31:50.1009688Z",
    "ObjectNo": 103464
},
{
    "Name": "Superman",
    "HasSecretIdentity": true,
    "Gender": "Male",
    "YearIntroduced": 1938
    "InsertedAt": "2018-08-08T18:31:50.1033607Z",
    "ObjectNo": 103468
}]
```

> The `InsertedAt` property of `Superhero` is read-only. They are included in `GET` request output, but cannot be set by
> remote clients. RESTable automatically includes the read-only Starcounter `ObjectNo` property for database resources.

## Exploring the parameters of `RESTableConfig.Init()`

The `RESTable.RESTableConfig.Init()` method has more parameters than the ones we used above. This is the complete
signature:

```csharp
static void Init
(
    ushort port = 8282,
    string uri = "/rest",
    bool requireApiKey = false,
    bool allowAllOrigins = true,
    string configFilePath = null,
    bool prettyPrint = true,
    ushort daysToSaveErrors = 30,
    LineEndings lineEndings = LineEndings.Windows,
    IEnumerable<EntityResourceProvider> entityResourceProviders = null,
    IEnumerable<IProtocolProvider> protocolProviders = null,
    IEnumerable<IContentTypeProvider> contentTypeProviders = null
);
```

For now, let's focus on `requireApiKey`, and `configFilePath`. These are used to control external access to the REST
API.

## Role-based authorization using API keys

In most use cases, we want to apply some form of role-based access control to the registered resources. Let's say only
some clients should be allowed to insert and delete `Superhero` entities, while all should be able to read. To implement
this, we create an XML file that will work as the configuration that RESTable reads API keys and access rights from.
Let's
create a new XML file in the project directory and call it "Config.xml". Let's make its content look like this:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<config>
  <ApiKey>
    <Key>a-secure-admin-key</Key>
    <AllowAccess>
      <Resource>RESTable.*</Resource>
      <Resource>RESTable.Admin.*</Resource>
      <Resource>RESTable.Dynamic.*</Resource>
      <Resource>RESTableTutorial.*</Resource>
      <Methods>*</Methods>
    </AllowAccess>
  </ApiKey>
  <ApiKey>
    <Key>a-secure-user-key</Key>
    <AllowAccess>
      <Resource>RESTableTutorial.*</Resource>
      <Methods>GET</Methods>
    </AllowAccess>
  </ApiKey>
</config>
```

This configuration file specifies two api keys: `a-secure-admin-key` and `a-secure-user-key`. The first can perform all
methods on all resources in the `RESTable`, `RESTable.Admin`, `RESTable.Dynamic` and `RESTableTutorial` namespaces, the
latter
which includes our `Superhero` resource. The second key, however, can only make `GET` requests to resources in
the `RESTableTutorial` namespace. To enforce these access rights, we set the `requireApiKey` parameter to `true` in the
call to `RESTableConfig.Init()` and provide the file path to the configuration file in the `configFilePath` parameter.
Here is the same application code as above, but now with role-based access control:

```csharp
public class TutorialApp
{
    public static void Main()
    {
        RESTableConfig.Init
        (
            port: 8282,
            uri: "/api",
            requireApiKey: true,
            configFilePath: Application.Current.WorkingDirectory + "/Config.xml"
        );
    }
}
```

## Non-starcounter resources

In the example above, we saw a Starcounter database class working as a REST resource through RESTable. Starcounter
database classes make for good examples, since most Starcounter developers are familiar with them, but RESTable itself
is
not limited to these classes. Any public non-static class can work as a RESTable resource class – as long as the
developer
can define the logic that is needed to support operations like `Select`, `Insert` and `Delete` that are used in REST
requests. Say, for example, that we want a REST resource that is simply a transient aggregation of database data, that
is generated when requested. To go with the example above, let's say we want a `SuperheroReport` class that we can
make `GET` requests like this
to: ([Postman](https://github.com/Mopedo/RESTable.Tutorial/blob/master/RESTableTutorial/Postman_report_get.jpg))

```
curl "localhost:8282/api/superheroreport" -H "Authorization: apikey a-secure-user-key"
Output:
[{
    "NumberOfSuperheroes": 2,
    "FirstSuperheroInserted": {
        "Name": "Wonder Woman",
        "HasSecretIdentity": false,
        "Gender": "Female",
        "YearIntroduced": 1941
        "InsertedAt": "2018-08-08T18:31:50.1009688Z",
        "ObjectNo": 103464
    },
    "LastSuperheroInserted": {
        "Name": "Superman",
        "HasSecretIdentity": true,
        "Gender": "Male",
        "YearIntroduced": 1938
        "InsertedAt": "2018-08-08T18:31:50.1033607Z",
        "ObjectNo": 103468
    }
}]
```

To implement `SuperheroReport`, just like we would with a database resource, we create a new .NET class, and assign
the `RESTableAttribute` attribute to it. This time we only need `GET` to be enabled for the resource. Note that the
class
below is not a Starcounter database class.

```csharp
namespace RESTableTutorial
{
    using RESTable;
    using Starcounter;
    using static RESTable.Methods;
    using System.Linq;
    using System.Collections.Generic;

    [RESTable(GET)]
    public class SuperheroReport : ISelector<SuperheroReport>
    {
        public long NumberOfSuperheroes { get; private set; }
        public Superhero FirstSuperheroInserted { get; private set; }
        public Superhero LastSuperheroInserted { get; private set; }

        public IEnumerable<SuperheroReport> Select(IRequest<SuperheroReport> request)
        {
            var superHeroesOrdered = Db
                .SQL<Superhero>("SELECT t FROM RESTableTutorial.Superhero t")
                .OrderBy(h => h.InsertedAt)
                .ToList();
            return new[]
            {
                new SuperheroReport
                {
                    NumberOfSuperheroes = superHeroesOrdered.Count,
                    FirstSuperheroInserted = superHeroesOrdered.FirstOrDefault(),
                    LastSuperheroInserted = superHeroesOrdered.LastOrDefault(),
                }
            };
        }
    }
}
```

To define or override the logic that is used when RESTable selects entities of a resource type, we implement
the `RESTable.ISelector<T>` interface, and use the resource type as the type parameter `T`. Failure to provide the
operations needed for the methods assigned in the `RESTableAttribute` constructor will result in a kind but firm runtime
exception. In the body of this `Select` method above, we provide logic for generating an `IEnumerable<SuperheroReport>`
that is then returned to RESTable when evaluating `GET` requests.

## Making requests

OK, now we've seen the basics of what RESTable can do – and how to make data sources from a Starcounter application
available over the REST API in a secure way. One of the really cool things about RESTable, which we haven't really
explored yet, is the flexibility and power it gives clients that consume the REST API. Included in RESTable is a wide
range of operations and utilities that make API consumption simple, powerful, fast and easy to debug. This tutorial
cannot possibly cover it all, but we'll provide some examples below.

We will use the same application as earlier, and imagine that the database is now populated with `Superhero` entities.
To try things out yourself – clone this repository to your local machine and run the `RESTableTutorial` application. The
application comes with an SQLite database that will automatically populate Starcounter with `Superhero` entities. If
that sounded cool, _which it totally is_, you should check
out [RESTable.SQLite](https://github.com/Mopedo/RESTable.SQLite)
after this.

### URI crash course

A RESTable URI consists of three parts after the service root, separated by forward slashes (`/`):

1. A resource locator, e.g. `superhero`. It points at a web resource.
2. A list of entity conditions that are either `true` or `false` of entities in the selected resource. The list items
   are separated with `&` characters. E.g. `gender=Female&HasSecretIdentity=false`. The key points to a property of the
   entity, and is not case sensitive. Values for string properties are always case sensititve.
3. A list of meta-conditions that define rules and filters that are used in the request. These list items are also
   separated with `&` characters. We can, for example, include `limit=2` here to limit the output to only two entities.

A complete description of all meta-conditions can be find in
the [specification](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Meta-conditions/), but here
are
some that are used below:

Name         | Function
:----------- | :-------------------------------------------------------------
`limit`      | Limits the output to a given number of entities
`offset`     | Skips a given number of entities
`select`     | Include only a subset of the entity's properties in the output
`add`        | Add a property to the output
`order_asc`  | Orders the output in ascending order by a given property
`order_desc` | Orders the output in descending order by a given property
`distinct`   | Returns only distinct entities (based on entity values)

Here is the main request template used
below: ([Postman](https://github.com/Mopedo/RESTable.Tutorial/blob/master/RESTableTutorial/Postman_template_get.jpg))

```
Method:   GET
URI:      http://localhost:8282/api
Headers:  Authorization: apikey a-secure-admin-key
```

The URIs below are all relative to the template URI. So the relative URI `/superhero` should be read
as `http://localhost:8282/api/superhero`

### Request examples

```
All superheroes:                            /superhero
The first 10 superheroes:                   /superhero//limit=10
Superheroes 15 to 20 (exclusive):           /superhero//limit=5&offset=14
All female superheroes:                     /superhero/gender=Female
5 male heroes with secret identities:       /superhero/gender=Male&hassecretidentity=true/limit=5
Female heroes introduced since 1990:        /superhero/gender=Female&yearintroduced>=1990
All male superhereoes' names:               /superhero/gender=Male/select=Name
  | + length of the name:                   /superhero/gender=Male/add=name.length&select=name,name.length
  | Ordered by name length:                 /superhero/gender=Male/add=name.length&select=name,name.length&order_asc=name.length
Years when a superhero was introduced:      /superhero//select=yearintroduced&distinct=true&order_asc=yearintroduced
Make a superhero report:                    /superheroreport
  | + weekday of first inserted as "Day"    /superheroreport//add=firstsuperheroinserted.insertedat.dayofweek&rename=firstsuperheroinserted.insertedat.dayofweek->Day
Get a compliment:                           /echo/Compliment=Well%20done%21%20Isn%27t%20this%20cool%3F%20Oh%2C%20sorry%2C%20did%20you%20think%20this%20would%20be%20a%20complement%20for%20you%3F
```

Note that we can use the `Length` .NET property of `System.String` in queries. All public instance properties (and
properties of properties) are available for references from meta-conditions like `add` and `select`.

Now, let's try getting some Excel files. For this, we set the `Accept` header
to `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`. Just `excel` will work too (you're welcome). For
Postman, set the `Accept` header and click the arrow to the right of the **Send** button, and then **Send and Download**
. This will save the Excel file to disk. Now try some of the requests above again!

## Conclusion

This concludes the tutorial. Hopefully you found some of it interesting and will continue by reading
the [specification](https://develop.mopedo.com/RESTable) and keep exploring what RESTable can do. If not, at least it's
over
now! [`¯\_(ツ)_/¯`](https://www.google.se/search?dcr=0&tbm=vid&ei=SvJ6Wt-KK4efsAG3rqjgCA&q=I+just+read+a+boring+tutorial%2C+can+I+have+some+cat+videos+or+something%3F&oq=I+just+read+a+boring+tutorial%2C+can+I+have+some+cat+videos+or+something%3F)

## Links

[• The RESTable specification](https://develop.mopedo.com/RESTable)

[• RESTable on NuGet](https://www.nuget.org/packages/RESTable/)

[• RESTable.SQLite](https://github.com/Mopedo/RESTable.SQLite)

[• Dynamit](https://github.com/Mopedo/Dynamit)

For any questions or comments about this tutorial, or anything RESTable-related, please contact Erik at
erik.von.krusenstierna@mopedo.com
