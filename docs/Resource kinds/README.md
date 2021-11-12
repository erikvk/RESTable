---
permalink: /Resource%20kinds/
---

# RESTable resource kinds

When using a RESTable API you may notice that there is are multiple **kinds** of resources that are available to you.

```
GET https://myapp.com/api/_/_/select=name,kind
Accept: application/json;raw=true
[
    {
        "Name": "RESTableTutorial.Superhero",
        "Kind": "EntityResource"
    },
    {
        "Name": "RESTable.OData.MetadataDocument",
        "Kind": "BinaryResource"
    },
    {
        "Name": "RESTable.Shell",
        "Kind": "TerminalResource"
    }
    ...
]
```

As we can see, there are three different values for the `Kind` properties of the listed resources. Let's go through them in order.

## Entity resources

Entity resources are the kind of web resource we should all be familiar with. They are modelled as sets of **entities** that can be represented using various [content types](../Consuming%20a%20RESTable%20API/Headers#content-type) – for example JSON, and that can be manipulated using common REST methods like `POST`, `PATCH` and `DELETE`. The semantics of entity resource interaction is, to a great extent, defined by the [HTTP protocol](https://en.wikipedia.org/wiki/Http). Entity resources are the most common resources in RESTable, and a large part of the documentation is specific to working with them. See also:

- [Consuming a RESTable API](../Consuming%20a%20RESTable%20API/Introduction)
- [Building entity resources](../Developing%20a%20RESTable%20API/entity%20resources)

## Binary resources

Binary resources contain read-only binary data of arbitrary size with a specific pre-defined content type. Just like with entity resources, we use HTTP to interact with binary resources. We can, however, only use the `GET` method. Unlike entity resources, the `Accept` request header is ignored when requesting a binary resource – we cannot request the content of a binary resource in any content type other than its native type.

Binary resources are useful when we have read-only document data – for example an XML document or a text file – that should be exposed over the REST API. A good example is the RESTable.OData [metadata document](https://github.com/Mopedo/RESTable.OData#metadata), which contains XML metadata for all resources in a RESTable application. See also:

- [Consuming a RESTable API](../Consuming%20a%20RESTable%20API/Introduction)
- [Building binary resources](../Developing%20a%20RESTable%20API/binary%20resources)

## Terminal resources

Terminal resources are small, single-purpose console applications that are hosted by a RESTable application, and that can be launched, controlled and terminated by API consumers. Once launched, they establish a two-way communication socket with the client, over which commands and data can be exchanged. While entity resources and binary resources are closely coupled with the HTTP protocol, which is what we use to interact with them, terminal resources have a similar coupling with another TCP protocol – the [WebSocket protocol](https://en.wikipedia.org/wiki/WebSocket).​

Terminal resources are useful when we want to provide high reactivity and interactivity between the server and the client – for example to build a real-time log resource that pushes data to multiple clients, or a command line interpreter. They exist to enable effective data interaction between client and server, whereas entity resources and binary resources are commonly used as sources of said data. See also:

- [Consuming terminal resources](../Consuming%20a%20RESTable%20API/Consuming%20terminal%20resources)
- [Building terminal resources](../Developing%20a%20RESTable%20API/terminal%20resources)