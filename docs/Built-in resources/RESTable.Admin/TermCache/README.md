---
permalink: RESTable/Built-in%20resources/RESTable.Admin/TermCache/
---

# `TermCache`

```json
{
    "Name": "RESTable.Admin.TermCache",
    "Kind": "EntityResource",
    "Methods": ["GET", "DELETE", "REPORT", "HEAD"]
}
```

RESTable caches the terms (parsed property locators) used in conditions and meta-conditions to speed up request handling. As an advanced administration feature, the `TermCache` can be used to control and debug this cache.

Format:

Property name | Type              | Description
------------- | ----------------- | -------------------------------------
Type          | `string`          | The type on which the terms is cached
Terms         | array of `string` | The terms cached for the type
