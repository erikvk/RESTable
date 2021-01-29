---
permalink: RESTable/Built-in%20resources/RESTable/Help/
---

# `Help`

```json
{
    "Name": "RESTable.Help",
    "Kind": "EntityResource",
    "Methods": ["GET", "REPORT", "HEAD"]
}
```

The `Help` resource contains merely a link to the [Consuming a RESTable API](../../Consuming%20a%20RESTable%20API/Introduction) section of this documentation.

## Example

```
GET https://my-server.com/rest/help
Response body: [
    {
        "DocumentationAvailableAt": "https://develop.mopedo.com/RESTable"
    }
]
```
