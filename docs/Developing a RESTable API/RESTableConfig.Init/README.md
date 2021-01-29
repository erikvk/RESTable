# `RESTableConfig.Init()`

This is the complete method signature for `RESTableConfig.Init()`:

```csharp
static void Init(
    ushort port = 8282,
    string uri = "/rest",
    bool requireApiKey = false,
    bool allowAllOrigins = true,
    string configFilePath = null,
    bool prettyPrint = true,
    ushort daysToSaveErrors = 30,
    LineEndings lineEndings = LineEndings.Windows,
    IEnumerable<ResourceProvider> resourceProviders = null,
    IEnumerable<IProtocolProvider> protocolProviders = null,
    IEnumerable<IContentTypeProvider> contentTypeProviders = null
);
```

By changing the values of these parameters, you can configure RESTable to best serve your current application.

## `port`

The `port` parameter controls which HTTP port to register the RESTable handlers on. One Starcounter HTTP handler is registered on this port for each of the following HTTP verbs: `GET, POST, PATCH, PUT, DELETE, OPTIONS`.

## `uri`

The `uri` parameter is what is used for the root uri of the REST API. If the application is deployed locally, and the `port` parameter is set to `8282`, the RESTable API will be listening for requests on `http://localhost:8282/rest`.

## `requireApikey`

To require authentication and authorization for all requests using [API keys](../../Administering%20a%20RESTable%20API/API%20keys), set this parameter to `true`. If set to `true`, it's required to include a [configuration file](../../Administering%20a%20RESTable%20API/Configuration) path in the [`configFilePath`](#configfilepath) parameter.

## `allowAllOrigins`

To require the administrator of the application to whitelist all CORS origins that are allowed to make requests to this application, set the `allowAllOrigins` parameter to `false`. If set to `false`, it's required to include a [configuration file](../../Administering%20a%20RESTable%20API/Configuration) path in the [`configFilePath`](#configfilepath) parameter.

## `configFilePath`

When either `requireApikey` is set to `true` or `allowAllOrigins` is set to `false`, RESTable needs a configuration file to read API keys and/or whitelisted CORS origins from. How to administrate API keys and CORS origins is covered in the [Administering a RESTable API](../../Administering%20a%20RESTable%20API/Introduction) section, but the path to the file needs to be provided by the developer.

## `prettyPrint`

The `prettyPrint` parameter controls whether JSON output from RESTable is "pretty printed", that is – indented – to increase human readability.

## `daysToSaveErrors`

When the REST API aborts operations due to some error, for example a format error in the incoming request, information about the error is stored in the `RESTable.Admin.Error` resource. The `daysToSaveErrors` parameter controls after how many days the error should be deleted from the log.

## `lineEndings`

By default, RESTable will use windows line endings, `\r\n`, in serialized JSON output. To change this to linux line endings, `\n`, set the value of `lineEndings` to `LineEndings.Linux`.

## `resourceProviders`

RESTable has support for add-ons in the form of [resource providers](../entity%20resources/Resource%20providers). They provide a way to standardize RESTable operations for some data storage technology – for example [SQLite](https://github.com/Mopedo/RESTable.SQLite). To include a resource provider in the RESTable instance, add it to a collection and assign to this parameter.

## `protocolProviders`

RESTable can also take add-ons in the form of [protocol providers](../Protocol%20providers), objects that contain the logic for parsing requests and generating responses according to some external protocol, for example [OData](https://github.com/Mopedo/RESTable.OData). To include a protocol provider in the RESTable instance, add it to a collection and assign to this parameter.

## `contentTypeProviders`

A third type of RESTable addon is [content type providers](../Content%20type%20providers). They add support for additional content types that can be used to read and write data from a RESTable application. Three content type providers ([JSON](../Content%20type%20providers#RESTablecontenttypeprovidersjsoncontentprovider), [Excel](../Content%20type%20providers#RESTablecontenttypeprovidersexcelcontentprovider), and [XML](../Content%20type%20providers#RESTablecontenttypeprovidersxmlwriter)) are already included with RESTable, but any additional providers must be added here.
