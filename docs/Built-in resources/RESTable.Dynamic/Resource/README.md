---
permalink: RESTable/Built-in%20resources/RESTable.Dynamic/Resource/
---

# `RESTable.Dynamic.Resource`

```json
{
    "Name": "RESTable.Dynamic.Resource",
    "Kind": "EntityResource",
    "Methods": ["GET", "POST", "PATCH", "PUT", "DELETE", "REPORT", "HEAD"]
}
```

The `RESTable.Dynamic` namespace contains all the procedurally generated resources that have been created for the current RESTable application. By default, the namespace contains a single resource, `RESTable.Dynamic.Resource`, a meta-resource that is used to create additional resources in the namespace. For an overview of dynamic resources, see [this section](../../../Administering%20a%20RESTable%20API/Dynamic%20resources).

`RESTable.Dynamic.Resource` is a meta-resource that contains entities representing all procedurally created resources (runtime resources) for the current RESTable application. Each entity in the resource corresponds with a dynamic Starcounter table created using the [Dynamit](https://github.com/Mopedo/Dynamit) library.

## Format

The format of `RESTable.Dynamic.Resource` is the same as for [`RESTable.Admin.Resource`](../../RESTable.Admin/Resource)
