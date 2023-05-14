## Lucene Query Syntax
This search supports Lucene Query Syntax. Please visit the offial documentation to get more details [https://lucene.apache.org/core/2_9_4/queryparsersyntax.html](https://lucene.apache.org/core/2_9_4/queryparsersyntax.html)

## Fields
The following fields are available for searching:

* `clientip`: The IP address of the client making the request.
* `method`: The HTTP method of the request (e.g. GET, POST, PUT).
* `url`: The full URL of the request, including query parameters.
* `path`: The path component of the URL.
* `request.header`: A specific header from the request.
* `request.header.HeaderName`: A specific header value from the request.
* `request.cookie`: A specific cookie from the request.
* `request.cookie.CookieName`: A specific cookie value from the request.
* `param`: A query parameter from the request.
* `param.ParamName`: A query parameter from the request.
* `request.body`: The body of the request.
* `status`: The HTTP status code of the response.
* `response.header`: A specific header from the response.
* `response.header.HeaderName`: A specific header value from the response.
* `response.body`: The body of the response.


## Examples

### Search for requests with a specific HTTP method:

```
method:GET
```

### Search for requests containing specific request header:

```
request.header:"UserAgent"
```

### Search for requests with a specific request header value:

```
request.header.UserAgent:"Mozilla"
```

### Search for request with specific request method and path

```
method:POST AND path:api/user
```