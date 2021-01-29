---
permalink: /Administering%20a%20RESTable%20API/JSON%20output%20formats/
---

# JSON Output formats

Sometimes when RESTable applications are consumed from other systems, for example dashboard systems like Bime or other RESTful web services, the JSON output from the RESTable application needs to conform to some external standard, for example [JSend](https://labs.omniti.com/labs/jsend). RESTable supports placing the JSON array produced by the `GET` request to some resource inside an arbitrarily defined JSON object structure - and the format can be set on a per-request basis using the [`format`](../../Consuming%20a%20RESTable%20API/URI/Meta-conditions#format) meta-condition. For more information, and how to work with JSON output formats, see the documentation for the [`RESTable.Admin.OutputFormat`](../../Built-in%20resources/RESTable.Admin/OutputFormat) resource.
