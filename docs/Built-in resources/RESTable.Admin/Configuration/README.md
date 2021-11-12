---
permalink: /Built-in%20resources/RESTable.Admin/Configuration/
---

# `Settings`

```json
{
    "Name": "RESTable.Admin.Configuraiton",
    "Kind": "EntityResource",
    "Methods": ["GET"]
}
```

The `Configuration` resource gives access to the settings of the REST API itself.

## Example

```
GET https://myapp.com/api/configuration
Accept: application/json;raw=true

Response body:

[{
	"Version": "1.0.25",
    "RunningExecutablePath": "C:\\Users\\erikv\\Source\\erikvk\\RESTable\\samples\\RESTable.Tutorial\\
bin\\Debug\\net6.0\\RESTable.Tutorial.exe",
    "NumberOfErrorsToKeep": 2000,
    "WebSocketBufferSize": 4096,
    "MaxNumberOfEntitiesInChangeResults": 100
}]
```
