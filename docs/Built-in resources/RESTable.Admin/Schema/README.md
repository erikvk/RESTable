---
permalink: /Built-in%20resources/RESTable.Admin/Schema/
---

# `Schema`

```json
{
    "Name": "RESTable.Admin.Schema",
    "Kind": "EntityResource",
    "Methods": ["GET", "REPORT", "HEAD"]
}
```

The `Schema` resource lets you print the schema for another resource. The schema contains names for all properties and types (C# namespaces and names) for the types used.

## Example

```
GET https://myapp.com/api/schema/resource=RESTable.Admin.Resource
Accept: application/json;raw=true
Response body: [{
    "Name": "System.String",
    "Description": "System.String",
    "EnabledMethods": "RESTable.Method[]",
    "IsDeclared": "System.Boolean",
    "IsProcedural": "System.Boolean",
    "IsInternal": "System.Boolean",
    "Type": "System.Type",
    "Views": "RESTable.ViewInfo[]",
    "IResource": "RESTable.Meta.IResource",
    "Provider": "System.String",
    "Kind": "RESTable.Meta.ResourceKind",
    "InnerResources": "System.Collections.Generic.IEnumerable`1[[RESTable.Admin.Resource, RESTable, Ve
rsion=1.0.25.0, Culture=neutral, PublicKeyToken=null]]"
}]
```