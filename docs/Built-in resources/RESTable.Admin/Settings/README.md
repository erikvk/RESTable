---
permalink: /Built-in%20resources/RESTable.Admin/Settings/
---

# `Settings`

```json
{
    "Name": "RESTable.Admin.Settings",
    "Kind": "EntityResource",
    "Methods": ["GET", "PATCH", "REPORT", "HEAD"]
}
```

The `Settings` resource gives access to the settings of the REST API itself. We can, for example, see the URI of the REST API, or change whether output JSON should be serialized with pretty printing or not.

## Format

Property name    | Type          | Description
---------------- | ------------- | -----------------------------------------------------------------------------
Port             | `integer`     | The port of the RESTable REST API
Uri              | `string`      | The URI of the RESTable REST API
PrettyPrint      | `boolean`     | Will JSON be serialized with pretty print? (indented JSON)
LineEndings      | `LineEndings` | The line endings to use when writing JSON
ResourcesPath    | `string`      | The path where resources are available
DocumentationURL | `string`      | The URL of the RESTable documentation
DaysToSaveErrors | `integer`     | The number of days to store errors in the [`RESTable.Error`](../Error) resource
