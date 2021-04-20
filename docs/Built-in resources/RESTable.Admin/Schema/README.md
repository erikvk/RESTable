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
GET https://my-server.com/rest/schema/resource=RESTable.Admin.Resource
Headers: 'Authorization: apikey mykey'
Response body: [{
    "Name": "System.String",
    "Alias": "System.String",
    "Description": "System.String",
    "EnabledMethods": "RESTable.Methods[]",
    "Editable": "System.Boolean",
    "IsInternal": "System.Boolean",
    "Type": "System.String",
    "Views": "RESTable.ViewInfo[]",
    "Provider": "System.String",
    "Kind": "RESTable.ResourceKind",
    "InnerResources": "RESTable.Admin.Resource[]"
}]
```
