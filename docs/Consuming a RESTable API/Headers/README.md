---
permalink: /Consuming%20a%20RESTable%20API/Headers/
---

# Headers

By including certain parameters as values in request headers, the client can instruct the server to perform some high-level operations while handling a request.

## `Authorization`

When designing a RESTable application, the developer can choose if to require API keys in REST API requests. If API keys are required, the client is expected to include the key in the `Authorization` header. If `"mykey"` is a valid API key, a request could look like this:

```
GET https://my-server.com/rest
Headers: 'Authorization: apikey mykey'
```

RESTable also supports [basic authentication](https://en.wikipedia.org/wiki/Basic_access_authentication). This is particularly useful when sending requests from clients that have built-in support for basic authentication. Basic authentication needs the client to input a user name and password. When sending requests to a RESTable API with basic authentication, leave the user name blank and use the api key as password.

## `Content-Type`

The `Content-Type` header is used to inform the server of what format the data from the data source is encoded in. Tha value should be a MIME type string, for example `application/json` (JSON) or `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (Excel). The RESTable application developer can define [additional supported input content types]().

## `Accept`

The `Accept` header tells the server to encode the response body in a certain format. By default, responses are JSON-formatted, but this can be changed to Excel by setting the content of the `Accept` header to `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`. The server will automatically, as per convention, include information in the `Content-Disposition` header, that the client can use to automatically give the output file an appropriate name. The RESTable application developer can define [additional supported output content types]().

### Example:

```
cd /Users/erik/Desktop
curl 'https://my-server.com/rest/employee'
-H 'Authorization: apikey mykey'
-H 'Accept: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' -O -J

# The -O flag saves the response as a file
# The -J flag uses the data in the Content-Disposition header to name the file

# Output: a new .xlsx file on my desktop called
# "MyResources.Employee_[time_code].xlsx" where [time_code] is substituted with the
# current date and time.
```
