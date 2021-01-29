---
permalink: RESTable/Built-in%20resources/RESTable.Admin/PropertyCache/
---

# `PropertyCache`

```json
{
    "Name": "RESTable.Admin.PropertyCache",
    "Kind": "EntityResource",
    "Methods": ["GET"]
}
```

The `PropertyCache` resource contains all the types and properties discovered by RESTable while working with the resources of the current RESTable application. It's useful when debugging RESTable applications.

## Format

Property name | Type              | Description
------------- | ----------------- | --------------------------------------------------------------
Type          | `string`          | The name of the type for which the properties have been cached
Properties    | array of `object` | The properties discovered for this type
