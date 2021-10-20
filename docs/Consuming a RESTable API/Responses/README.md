---
permalink: /Consuming%20a%20RESTable%20API/Responses/
---

# Responses

## Error responses

If something goes wrong while evaluating a request, the REST API will respond with either a `4XX` or `5XX` status code and a description of the error in the [`RESTable-info`](#RESTable-info) response header. Errors that are not `403: Forbidden` are also stored in the [`RESTable.Error`](../../Built-in%20resources/RESTable.Admin/Error) resource. In addition to this, the response body will be a representation of the error that occured.

## Success responses

### GET

Code | Status description | Body     | Info
---- | ------------------ | -------- | ---------------------------------------------------
200  | OK                 | entities | If entities are found, representations are included
204  | No content         | _empty_  |

### POST

Code | Status description | Body    | Notes
---- | ------------------ | ------- | ----------------------------
201  | Created            | _empty_ |
200  | OK                 | _empty_ | If no entities was inserted.

### PATCH

Code | Status description | Body    | Notes
---- | ------------------ | ------- | -------------------------------
200  | OK                 | _empty_ | Even if no property was changed

### PUT

Code | Status description | Body    | Notes
---- | ------------------ | ------- | ------------------------
201  | Created            | _empty_ |
200  | OK                 | _empty_ | If an entity was updated

### DELETE

Code | Status description | Body    | Notes
---- | ------------------ | ------- | -----
200  | OK                 | _empty_ |

### REPORT

Code | Status description | Body        | Notes
---- | ------------------ | ----------- | -----
200  | OK                 | report body |

All successful responses from `REPORT` requests share the same body format:

Property name | Type      | Description
------------- | --------- | ----------------------------------------------
Count         | `integer` | The number of entities selected by the request

### HEAD

Code | Status description | Body    | Info
---- | ------------------ | ------- | ---------------------
200  | OK                 | _empty_ | Only response headers
204  | No content         | _empty_ |

## Custom response headers

RESTable uses the following custom HTTP headers to include meta-data in responses. These do not include standard HTTP headers like `Content-Type`, `Content-Length` etc.

### `RESTable-info`

Information about the result of the request, if any. For `POST` requests, for example, `RESTable-info` contains the number of inserted entities. For any error response, a description of the error.

### `RESTable-error`

For error responses, `RESTable-error` contains a link to the [`RESTable.Admin.Error`](../../Built-in%20resources/RESTable.Admin/Error) entity describing this particular error. For all other responses, this header is excluded.

### `RESTable-elapsed-ms`

The number of milliseconds elapsed during the evaluation of the request.

### `RESTable-version`

The version of the RESTable package of the RESTable application that generated the response.