---
permalink: RESTable/Built-in%20resources/RESTable.Admin/WebhookLogSettings/
---

# `WebhookLogSettings`

```json
{
    "Name": "RESTable.Admin.WebhookLogSettings",
    "Kind": "EntityResource",
    "Methods": ["GET", "PATCH", "DELETE", "REPORT", "HEAD"]
}
```

The `WebhookLogSettings` resource contains settings for how [webhook log items](../WebhookLog) are handled in the RESTable application. To return to defaults, delete the single entity in this resource.

## Format

_Properties marked in **bold** are required._

Property name      | Type                                     | Description
------------------ | ---------------------------------------- | ----------------------------------------------------------------------
LastCleared        | [`datetime`](../../Datetime) (read-only) | The date and time when old items was last cleared from the webhook log
DaysToKeepLogItems | `integer`                                | The number of days to keep webhook log items
