---
permalink: /Built-in%20resources/RESTable/Echo/
---

# `Echo`

```json
{
    "Name": "RESTable.Echo",
    "Kind": "EntityResource",
    "Methods": ["GET", "REPORT", "HEAD"]
}
```

RESTable.Echo is a test and utility resource that simply returns the request URI conditions as an object, where conditions keys are property names, and condition values are property values.

## Example:

```
GET https://myapp.com/api/echo/Name=Erik&Job=Developer&Other%20thing!=null
Accept: application/json;raw=true
Response body:
[{
    "Name": "Erik",
    "Job": "Developer",
    "Other thing": null
}]
```
