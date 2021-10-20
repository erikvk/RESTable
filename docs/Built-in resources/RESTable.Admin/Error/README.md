---
permalink: /Built-in%20resources/RESTable.Admin/Error/
---

# `Error`

```json
{
    "Name": "RESTable.Admin.Error",
    "Kind": "EntityResource",
    "Methods": ["GET", "DELETE", "REPORT", "HEAD"]
}
```

When the REST API aborts operations due to some error, information about the error is stored in the `Error` resource. `Error` entities contain information to help debug requests and resources, and are useful when encountering unknown errors. Erronous responses will contain a link to the corresponding `Error` entity in the `ErrorInfo` header for easy traceability.

## Format

Property name | Type                         | Description
------------- | ---------------------------- | --------------------------------------------------
Id            | `string`                     | The ID of the `Error`
Time          | [`datetime`](../../Datetime) | The date and time when the `Error` was encountered
ResourceName  | `string`                     | The resource that threw the exception
Action        | `string`                     | The action that failed (e.g. the method `GET`)
ErrorCode     | [`ErrorCode`](../ErrorCode)  | The `ErrorCode` of the `Error`
StackTrace    | `string`                     | The stack trace of the encountered exception
Message       | `string`                     | The message of the encountered exception
Uri           | `string`                     | The uri of the request
Headers       | `string`                     | The headers of the request
Body          | `string`                     | The body of the request

## Example

Request:

```
GET https://myapp.com/api/thisisnotaresource
```

Response:

```json
{
  "Status": "fail",
  "ErrorType": "RESTable.Results.UnknownResource",
  "ErrorCode": "UnknownResource",
  "Message": "RESTable could not locate any resource by 'thisisnotaresource'.",
  "MoreInfoAt": "/restable.admin.error/id=1",
  "TimeStamp": "2021-10-20T13:01:23.8870186Z",
  "Uri": "/thisisnotaresource",
  "TimeElapsedMs": 0
}
```

Error lookup request:

```
GET https://myapp.com/api/restable.admin.error/id=1
```

Error lookup response body:

```
{
  "Status": "success",
  "Data": [
    {
      "Id": 1,
      "Uri": "/thisisnotaresource",
      "Method": "GET",
      "Headers": "Accept: */* | Host: localhost:5001 | User-Agent: curl/7.55.1",
      "Body": "",
      "ResourceName": "<unknown>",
      "Time": "2021-10-20T13:03:49.3631383Z",
      "ErrorCode": "UnknownResource",
      "StackTrace": "at RESTable.Meta.ResourceCollection.FindResource(String searchStri... etc."
    }
  ],
  "DataCount": 1,
  "TimeElapsedMs": 10.851
}
```

The `StackTrace` is useful to include in bug reports to developers.
