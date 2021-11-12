---
permalink: /Consuming%20a%20RESTable%20API/Body%20and%20data%20sources/
---

# Body and data sources

`POST`, `PATCH` and `PUT` requests require a data source to insert and/or update resources from, and expects this data in the body of the request.

## POST using JSON text

```bash
curl -X POST -d '
{
    "Name": "Karen Smith",
    "DateOfEmployment": "2013-01-23T00:00:00",
    "Salary": 45000
}
' 'https://myapp.com/api/employee' -H 'Authorization: apikey mykey' -H 'Content-Type: application/json'
```