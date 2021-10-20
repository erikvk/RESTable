---
permalink: /Administering%20a%20RESTable%20API/CORS/
---

# CORS

If you are unfamiliar with CORS (Cross-Origin Resource Sharing), check out this [excellent introduction](https://www.codecademy.com/articles/what-is-cors)

RESTable has built-in support for handling incoming CORS requests, and allows the administrator to set up a pre-defined list of whitelisted origins that should be allowed to make such request. For applications that accept whitelisting of CORS origins, the administrator can add such origins by including an [`AllowedCorsOrigins`](../Configuration#allowedcorsorigins) string array inside the app configuration file.
