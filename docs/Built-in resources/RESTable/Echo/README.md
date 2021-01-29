---
permalink: RESTable/Built-in%20resources/RESTable/Echo/
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
GET https://my-server.com/rest/echo/Name=Erik&Job=Developer&Other%20thing!=null
Headers: 'Authorization: apikey mykey'
Response body:
[{
    "Name": "Erik",
    "Job": "Developer",
    "Other thing": null
}]
```
