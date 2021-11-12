---
permalink: /Built-in%20resources/RESTable/Aggregator/
---

# `Aggregator`

```json
{
    "Name": "RESTable.Aggregator",
    "Kind": "EntityResource",
    "Methods": ["GET", "REPORT", "HEAD"]
}
```

The `Aggregator` resource is used to create custom aggregated datasets from potentially multiple requests to the web resources of a RESTable application. It accepts a [request template](#request-templates) as input – which is included as [body](../../Consuming%20a%20RESTable%20API/Body%20and%20data%20sources) of `GET` requests. It then parses the request template and makes internal API requests for all [request literals](#request-literals) contained in the template. Then the results from these API requests, which is either an object array (for `GET` requests) or an integer (for `REPORT` requests), are substituted for t he request literals in the request template. Finally the populated request template is returned as output and sent back to the client.

## Example

```
GET https://myapp.com/api/aggregator
Accept: application/json;raw=true
Body: {
    "NrOfResources": "REPORT /resource",
    "ResourceNames": "/resource//select=name", // GET is inferred
    "PleaseIgnoreThis": 123
}
Response body: [{
    "NrOfResources": 39,
    "ResourceNames": [
        {
            "Name": "RESTable.Admin.AdminTools"
        },
        {
            "Name": "RESTable.Admin.DatabaseIndex"
        },
        {
            "Name": "RESTable.Admin.Error"
        },
        {
            "Name": "RESTable.Admin.ErrorCode"
        },
        {
            "Name": "RESTable.Admin.Macro"
        }
    ],
    "PleaseIgnoreThis": 123
}]

// REPORT requests are reduced to integers
// GET requests are reduced to JSON arrays of objects
```
