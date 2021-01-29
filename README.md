# What is RESTable?

RESTable is a REST API framework for .NET applications, that is free to use and easy to set up in new or existing applications. Its purpose is to provide a set of high-quality tools for developers to build great REST APIs, without having to deal with the complexities (and sometimes utter boringness) associated with client authentication, request processing, data integrations, performance optimizations, URI conventions and documentation.

> If the term "REST API" is unfamiliar to you, we recommend that you read this [excellent tutorial](](docs/http://www.restapitutorial.com), that covers all you need to know.

Using RESTable tools, developers can quickly build powerful REST APIs that, among other things:

1. Authenticate and authorize clients using [role-based access control](](docs/Administering%20a%20RESTable%20API/API%20keys) and API keys
2. [Create web resources](](docs/Developing%20a%20RESTable%20API/Registering%20resources) from Sdatabase tables or any other persistent or transient data
3. Allow external clients to [interact with the resources](](docs/Consuming%20a%20RESTable%20API/Introduction) using standard HTTP methods like `GET` and `POST`, and content types like JSON and XML
4. Allow [advanced queries](](docs/Consuming%20a%20RESTable%20API/Request%20overview/#examples) like filtering, renaming, searching and ordering of resource representations before they are returned to the client
5. Handle and categorize errors during request evaluation for easy [remote debugging](](docs/Built-in%20resources/RESTable.Admin/Error)
6. Allow clients to [set up database indexes](](docs/Built-in%20resources/RESTable.Admin/DatabaseIndex) to optimize database queries
7. Can save any data to an [Excel file](](docs/Consuming%20a%20RESTable%20API/Headers#accept)
8. Allow clients to set up HTTP callbacks using [webhooks](](docs/Administering%20a%20RESTable%20API/Webhooks) that listen for [custom events](](docs/Resource%20kinds/Event%20resources)
9. Have a built-in [WebSocket shell](](docs/Built-in%20resources/RESTable/Shell) that clients can use to explore and consume the REST API, as well as support for [custom websocket resources](](docs/Developing%20a%20RESTable%20API/Terminal%20resources)

RESTable is a continuation of the work done in the excellent REST API framework [RESTar](https://github.com/Opportunity-Holding/RESTar) for Starcounter applications.