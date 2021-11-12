---
permalink: /Consuming%20a%20RESTable%20API/Consuming%20terminal%20resources/
---

# Consuming terminal resources

Terminal resources are small single-purpose console applications that can be launched and closed by REST clients. We interact with terminal resources using the WebSocket protocol, and to launch a terminal resource, we make a `GET` request to the given resource, and include a [WebSocket handshake](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers#The_WebSocket_Handshake) – so that the server can initiate a WebSocket connection for the terminal.

The easiest way to initiate a WebSocket connection with a RESTable API while testing, is to use a command line tool like [`wscat`](https://www.npmjs.com/package/wscat), but something like [this Chrome extension](https://chrome.google.com/webstore/detail/simple-websocket-client/pfdhoblngboilpfeibdedpjgfnlcodoo?hl=en) works fine too if you prefer to work with a web browser.

To test things out, let's make a request to the [`RESTable.Shell`](../../Built-in%20resources/RESTable/Shell) terminal resource on RESTable service. For this we can use this `wscat` command:

```
wscat -c "wss://myapp.com/api/shell"
```

This will launch the `RESTable.Shell` terminal resource, which will let you navigate around the available resources of the server. For more information about the `RESTable.Shell` resource, see [this documentation](../../Built-in%20resources/RESTable/Shell).

## General features of terminal resources

Terminal resources are small, encapsulated command-line applications, that are instantiated for each request that target them. Each such instance can run independently from other instances. Each connected WebSocket has exaxly one terminal resource assigned to it at all times. RESTable will instantiate terminal resources and assign WebSockets to them – a terminal resource can never assign itself to some WebSocket. When a terminal is done with a given WebSocket connection, it must redirect it to a different terminal, for example [`RESTable.Shell`](../../Built-in%20resources/RESTable/Shell). The Shell is the only terminal resource that can close WebSocket connections. The client can always close the terminal by either using a [global command](#global-commands) or by simply closing the WebSocket connection from their end.

## WebSocket headers

Each WebSocket connected to RESTable has a set of headers, that are copied from the WebSocket upgrade request. In the case of the [`RESTable.Shell`](../../Built-in%20resources/RESTable/Shell) resource resource, these are also used in all subsequent requests from that resource. We can read or modify the headers of the WebSocket by using the [`#INFO` global command](#global-commands).

## Terminal resource properties

Each terminal resource declares a set of instance properties that define its state. A common use case for these properties is to define settings that determine the behavior of the terminal resource. Just like with properties of [entity resources](../../Resource%20kinds#entity-resources), terminal resource properties can be read-only. Properties of terminal resources should be documented along with the terminal resource.

There are two ways to set the values of a terminal resource's properties.

1. By including an assignment as a condition in the initial `GET` request to the terminal resource.
2. By using the [`#TERMINAL` global command](#global-commands) with a JSON object as argument.

The example below will demonstrate both these methods.

### Example

Let's look at the properties of the [`RESTable.Shell`](../../Built-in%20resources/RESTable/Shell) resource:

Property name    | Type      | Description
---------------- | --------- | ---------------------------------------------------------------------------------------------------------
Query            | `string`  | Determines the current location of the shell, and the URI for subsequent requests
Unsafe           | `boolean` | Is the shell currently in unsafe mode? (see the [`unsafe` meta-condition](../URI/Meta-conditions#unsafe))
WriteHeaders     | `boolean` | Should the shell include headers when writing output?
AutoOptions      | `boolean` | Should the shell automatically send an `OPTIONS` command after each successful navigation?
AutoGet          | `boolean` | Should the shell automatically send an `GET` command after each successful navigation?

Each instance of this terminal resource will contain its own set of values for these properties. They act as the settings of the `Shell` instance.

We can instantiate the `Shell` resource by making a WebSocket upgrade request with `/shell` as [URI](../URI), and we can optionally include assignments to the terminal's properties by adding [conditions](../URI/Conditions) to the URI. To launch a `Shell` instance with `AutoGet` and `WriteHeaders` both set to `true`, for example, we can use this URI with the WebSocket upgrade request:

```
/shell/autoget=true&writeheaders=true
```

We can also use the [`#TERMINAL` global command](#global-commands) to set the properties of the current terminal once it's launched. As argument we use a JSON object describing the changes to make. To set `AutoGet` and `WriteHeaders` to `true` for a running `Shell` instance, we can run the following global command:

```
> #TERMINAL {"AutoGet": true, "WriteHeaders": true}
< Terminal updated
< {
  "Query": "/console",
  "Unsafe": false,
  "WriteHeaders": true,
  "AutoOptions": false,
  "AutoGet": true,
  "StreamBufferSize": 16000000
}
```

For more information on global commands, see the next section.

## Global commands

Global commands are text commands available in all terminal resources, that lets the client perform high-level operations such as setting terminal properties, switching terminals and get metadata about the WebSocket connection.

### `#TERMINAL`

Returns the properties of the current terminal. An optional JSON object can be used as argument to update the properties of the terminal. See [this section](#example) for an example.

### `#INFO`

Returns an object describing the current WebSocket connection. An optional JSON object can be used as argument to update the contents of the object. The returned object has the following properties:

Property name   | Type                 | Default value           | Description
--------------- | -------------------- | ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------
Host            | `string` (read-only) | `""`                    | The host of the WebSocket connection
WebSocketId     | `string` (read-only) | `""`                    | The unique ID of the WebSocket connection
IsSSLEncrypted  | `boolean`(read-only) | `false`                 | Is the WebSocket connection secure?
ClientIP        | `string` (read-only) | `"0.0.0.0"`             | The IP of the connected client
ConnectedAt     | `string` (read-only) | `"0001-01-01 00:00:00"` | The date and time when the connection was established
CurrentTerminal | `string` (read-only) | `""`                    | The type name of the current terminal resource attached to this WebSocket
CustomHeaders   | `object`             | `{}`                    | The custom headers for this WebSocket. Other headers, such as [`Authorization`](../Headers#authorization), cannot be viewed or modified here

The content of `CustomHeaders` can be changed. To add a header `X-MyHeader` with value `"MyValue"`, we can use the following global command:

```
> #INFO {"CustomHeaders": {"X-MyHeader": "MyValue"}}
< Profile updated
< {
  "Host": "localhost:5001",
  "WebSocketId": "Aw",
  "IsEncrypted": true,
  "ClientIP": "127.0.0.1",
  "ConnectedAt": "2021-10-20 15:44:31",
  "CurrentTerminal": "RESTable.Shell",
  "CustomHeaders": {
    "X-MyHeader": "MyValue"
  }
}
```

### `#SHELL`

Running this command redirects the WebSocket to the [`RESTable.Shell`](../../Built-in%20resources/RESTable/Shell) resource.

### `#DISCONNECT`

Running this command closes the WebSocket connection and disposes all attached resources.