---
permalink: /Built-in%20resources/RESTable.Admin/ErrorCode/
---

# `ErrorCode`

```json
{
    "Name": "RESTable.Admin.ErrorCode",
    "Kind": "EntityResource",
    "Methods": ["GET", "REPORT", "HEAD"]
}
```

The error codes used by RESTable have their own resource, which is useful for administrators wanting to check some error code that appears in [`Error`](../Error) entities.

## Example

```
GET https://myapp.com/api/errorcode/code<5
Response body:
[
    {
        "Name": "NoError",
        "Code": -1
    },
    {
        "Name": "Unknown",
        "Code": 0
    },
    {
        "Name": "InvalidUriSyntax",
        "Code": 1
    },
    {
        "Name": "InvalidMetaConditionValueType",
        "Code": 2
    },
    {
        "Name": "InvalidMetaConditionOperator",
        "Code": 3
    },
    {
        "Name": "InvalidMetaConditionSyntax",
        "Code": 4
    }
]
```
