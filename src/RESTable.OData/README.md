_By Erik von Krusenstierna (erik.von.krusenstierna@mopedo.com)_

# What is RESTable.OData?

RESTable.OData is a free to use open-source protocol provider for [RESTable](https://develop.mopedo.com/RESTable/) that makes it possible to interact with a RESTable API using the [OData 4.0 protocol](http://www.odata.org/). Requests to RESTable.OData are compiled into a common format before evaluation, which means that it can use all the facilities of RESTable, just over a different interface.

RESTable.OData is meant to be a _community project_, and Mopedo will provide the initial help needed for further development as well as regular updates to match the latest releases of RESTable. If you have any questions or issues, or wish to contribute to the project, [post an issue](https://github.com/Mopedo/RESTable.OData/issues) or contact Erik at erik.von.krusenstierna@mopedo.com.

## Getting started

RESTable.OData is, like RESTable, distributed as a [package](https://www.nuget.org/packages/RESTable.OData) on the NuGet Gallery, and an easy way to install it in an active Visual Studio project is by entering the following into the NuGet Package Manager console:

```
Install-Package RESTable.OData
```

## Using RESTable.OData

RESTable.OData defines a **protocol provider** for RESTable, which should be included in the call to [`RESTableConfig.Init()`](https://develop.mopedo.com/RESTable/Developing%20a%20RESTable%20API/RESTableConfig.Init/) in applications that wish to use it. Protocol providers are essentially add-ons for RESTable, enabling – for example – API protocols like OData to work as a native protocol for RESTable – and interact with RESTable resources just like the built-in protocol. For more on protocol providers, see the [RESTable Specification](https://develop.mopedo.com/RESTable/Developing%20a%20RESTable%20API/Protocol%20providers/).

For information about the OData protocol, see the [OData documentation](http://www.odata.org/documentation/).

### Example requests

To specify the protocol for a RESTable request, we add a dash `-` and the protocol ID directly after the root URI of the service. In RESTable.OData's case, the ID is `odata`. Here are some OData requests URIs, and their equivalent RESTable protocol URIs.

```
OData:  GET http://localhost:8282/api-odata/superhero
RESTable: GET http://localhost:8282/api/superhero
```

```
OData:  GET http://localhost:8282/api-odata/superhero?$filter=hassecretidentity%20eq%20true&$top=5
RESTable: GET http://localhost:8282/api/superhero/hassecretidentity=true/limit=5
```

```
OData:  GET http://localhost:8282/api-odata/superhero?$orderby=name%20asc
RESTable: GET http://localhost:8282/api/superhero//order_asc=name
```

## Supported operations

### URIs and operations

RESTable.OData supports the following OData query options:

- `$filter` – equivalent to [URI conditions](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Conditions/) in RESTable protocol
- `$orderby` – equivalent to the [`order_asc`](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Meta-conditions/#order_asc) and [`order_desc`](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Meta-conditions/#order_desc) meta-conditions
- `$select` – equivalent to the [`select`](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Meta-conditions/#select) meta-condition
- `$skip` – equivalent to the [`offset`](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Meta-conditions/#offset) meta-condition
- `$top` – equivalent to the [`limit`](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Meta-conditions/#limit) meta-condition

#### `$filter`

RESTable.OData supports the following operators in `$filter` query option conditions:

- `"eq"`– equivalent to `"="` (equals)
- `"ne"`– equivalent to `"!="` (not equals)
- `"lt"`– equivalent to `"<"` (less than)
- `"gt"`– equivalent to `">"` (greater than)
- `"le"`– equivalent to `"<="` (less than or equals)
- `"ge"`– equivalent to `">="` (greater than or equals)

The operators available in the RESTable protocol are coverered [here](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/URI/Conditions/#operators).

#### `$orderby`

RESTable.OData supports only one argument per `$orderby` query option, and only one `$orderby` query option per URI.

### Content types

RESTable.OData has support for only `application/xml` when writing the Metadata document, and only `application/json` when writing all other resources. RESTable.OData only accepts `application/json` as input format.

### Methods

RESTable.OData supports all [methods](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/Methods/) that are enabled for the resource, except [`REPORT`](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/Methods/#report). So OData clients can select, insert, update and delete entities just like with the RESTable protocol.

### Authentication/authorization

RESTable.OData supports [authentication and access control](https://develop.mopedo.com/RESTable/Consuming%20a%20RESTable%20API/Headers/#authorization), just like in the RESTable protocol.

### Metadata

RESTable.OData publishes an OData metadata document at `/$metadata`, with a few things to mention:

- Metadata is generated by RESTable's own reflection, which is available at `RESTable.Metadata.Get()`, which means no further member reflection is needed to generate the metadata document.
- Navigation properties are not currently used. Instead all properties have `<Property>` tags.
- Unlike RESTable, OData has a formal notion of **key**, that is used in entity types. All entity types should have a key, which requires the developer to declare which member in the resource type that should be treated as key. This is done by decorating the member with the `System.ComponentModel.DataAnnotations.KeyAttribute`.
- All Starcounter database resources have their `ObjectNo` as key.
- All SQLite database resources have their `RowId` as key.
