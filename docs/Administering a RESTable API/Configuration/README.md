---
permalink: /Administering%20a%20RESTable%20API/Configuration/
---

# Configuration

RESTable can read its configuration from the appsettings.json configuration file. This includes **API keys** and allowed **CORS origins** that are used by RESTable to control access to the API. The developer will define where the file is to be stored, and the administrator makes sure that the file contains the correct keys and origins.

The developer can also choose to bind the API keys configuration to a config section different to the default `RESTable.ApiKeys` as shown here.

## Configuration file example

```json
{
  "RESTable.ApiKeys": [
    {
      "ApiKey": "myadminkey",
      "AllowAccess": [
        {
          "Resources": [
            "RESTable.*",
            "RESTable.Admin.*",
            "MyApp.*"
          ],
          "Methods": [
            "*"
          ]
        }
      ]
    },
    {
      "ApiKey": "myotherkey",
      "AllowAccess": [
        {
          "Resources": [
            "MyApp.*"
          ],
          "Methods": [
            "GET", "POST"
          ]
        }
      ]
    }
  ],
  "RESTable.AllowedCorsOrigins": ["http://mysite.com", "https://mysite.com"]
}
```

Here, `RESTable.ApiKeys` defines the array of API keys that are used in the RESTable application. Inside this array we have the `ApiKey` objects, each describing a unique API key.

## `ApiKey`

The `ApiKey` object inside the `ApiKeys` array node is used to assign access rights to an [API key](../API%20keys).

It has four inner items:

1. [`ApiKey`](#key), defining the character string that will be used as an API key
2. [`AllowAccess`](#allowaccess), defining a set of resources along with a set of REST methods that this key can use for these resources
3. [`Resources`](#resource), used in `AllowAccess` objects to include one or more resources in the access right assignment
4. [`Methods`](#methods), used in `AllowAccess` objects to assign a set of REST methods to a set of selected resources

Each `ApiKey` must contain **exacly one** `ApiKey` string, and **at least one** `AllowAccess` object.

### `ApiKey`

The `ApiKey` is a character string that is used as an API key in the REST API. It has the following syntactical limitations:

1. Must contain at least one ASCII character.
2. Must only contain ASCII characters within the range [33 through 126](https://ascii.cl/).
3. Must not contain parentheses characters, i.e. `'('` or `')'`

### `AllowAccess`

Each `AllowAccess` object assigns additional access rights to an API key. Each `AllowAccess` object must contain **at least one** `Resource` item.

The total **access scope**, defined here as a set of method-resource pairs such that the key can be used with the method for the resource, for a key `k` is equal to the set union of all method-resource pairs assigned in the `AllowAccess` nodes for `k`. This means that we cannot exclude resources or methods from the scope of `k` by simply adding additional `AllowAccess` nodes with smaller sets of resources and/or methods.

### `Resources`

A `Resources` array defines one or more resources that should be included in the scope of an `AllowAccess` object. It contains strings consisting either of the full name of a REST resource, or of a resource namespace followed by a dot and the wildcard character `*`. The wildcard character is used to include multiple resources from a namespace, excluding inner namespaces. If we have the following resources in our REST API...

```
MyApp.Person
MyApp.Admin.Log
MyApp.Admin.Settings
```

... the following `Resources` array will select both the `MyApp.Admin.Log` and `MyApp.Admin.Settings` resources:

```
Resources: ["MyApp.Admin.*"]
```

The following node...

```
Resources: ["MyApp.*"]
```

... will select `MyApp.Person`, but not any of the resources from the `MyApp.Admin` namespace. To include all resources, we must use this `Resources` array:

```
Resources: ["MyApp.*", "MyApp.Admin.*"]
```

### `Methods`

The `Methods` array contains all the methods to assign to the set of resources selected by the `Resources` array in the containing `AllowAccess`. The `Methods` array contains strings, each being either a [REST method](../../Consuming%20a%20RESTable%20API/Methods), e.g. `"GET", "POST"` or `"DELETE"`, or the `*` character, which is equivalent to `GET, POST, PATCH, PUT, DELETE, REPORT, HEAD`.

Note that the `REPORT` and `HEAD` methods are automatically made available if `GET` is made available.

## `AllowedCorsOrigins`

`AllowedCorsOrigins` is an array of strings, each containing an origin that is allowed to make CORS requests. The value is simply the URL of the origin to whitelist. If the RESTable application is set up to allow requests from all CORS origins, whitelisted origins will be ignored.

### Examples

To allow CORS requests from `mysite.com`, which can be accessed using the `HTTP` and `HTTPS` URL schemes, we add the following the the configuration file:


```json
"RESTable.AllowedCorsOrigins": ["http://mysite.com", "https://mysite.com"]
```
