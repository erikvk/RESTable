# Content type providers

A content type provider defines serialization and deserialization operations for a **content type**. Content types are data formats that can be used in RESTable input and output, and using content type providers – we can add additional content types to a RESTable application.

Content type providers bind logic to a [MIME type](https://en.wikipedia.org/wiki/Media_type), which is defined as a string, for example `application/json`. When receiving requests, RESTable will pick the appropriate content type provider to use when deserializing input data from the content of the [`Content-Type`](../../Consuming%20a%20RESTable%20API/Headers#content-type) header, and the appropriate content type provider to use when serializing output data from the content of the [`Accept`](../../Consuming%20a%20RESTable%20API/Headers#accept) header.

## Included content type providers

These three protocol providers are already included with RESTable. You can also list all available protocols and their content providers by making a `GET` request to the `RESTable.Admin.Protocol` resource.

### `RESTable.Json.SystemTextJsonProvider`

The `SystemTextJsonProvider` is the default JSON content type provider for reading and writing JSON, and handles requests that have the following MIME types defined in their [`Accept`](../../Consuming%20a%20RESTable%20API/Headers#accept) or [`Content-Type`](../../Consuming%20a%20RESTable%20API/Headers#content-type) headers:

```
application/json
application/RESTable-json
json
text/plain
```

Since custom content type providers can override bindings to MIME types, a special `application/RESTable-json` is used to preserve a binding, should the client need to use the `JsonContentProvider` while `application/json` is overridden.

There is an `IJsonProvider` service available from the application's service provider, that can be used in RESTable applications to serialize, deserialize and populate JSON according to RESTable metadata.

### `RESTable.Excel.ExcelContentProvider`

The `ExcelContentProvider` is used to read and write Excel files. It has the following MIME type bindings:

```
application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
application/RESTable-excel
excel
```

### `RESTable.Json.JsonLinesProvider`

The `JsonLinesProvider` can be used to write responses to JsonLines (see [JSON Lines](https://jsonlines.org/) for more information).

## Creating custom content type providers

To create a custom content type provider, implement the `IContentTypeProvider` interface and add a registration for `IContentTypeProvider` with your implementation in the application's service collection on startup.