
# RESTar (alpha) - an audacious REST Api for Starcounter
# This readme will be included as a resource in RESTar in a future version
# RESTar is a work in progress, contact Erik at erik.von.krusenstierna@mopedo.com with questions, comments or issues
# See the example project for more concrete implementation details and guidelines

# SECTIONS:
    1. Commands
    2. Resources

    3. Methods
    4. Safe and unsafe commands
    5. Examples

- - -

1. Requests

RESTar take HTTP requests as input, but extends the URI syntax to express more detailed interactions with its resources. Requests to RESTar have the following components:

    Method: [GET|POST|PUT|PATCH|DELETE]

    URI: http://[ip_address][:port]/[base_uri][/resource_locator][/conditions][/meta-conditions]

    Headers: [ExternalSource: URI],

    Data: [JSON object|JSON object array]

Some URI instances:

http://10.0.1.12:8200/my_uri/device
http://10.0.1.12:8200/my_uri/user/id=200/select=name,id
http://10.0.1.1/my_uri/device//limit=1
http://10.0.1.1/my_uri///order_desc=nrofrows

For more examples, see 'Examples' section.

1.1. ip_address 

The IP address used to reach your Starcounter application.

1.2. port

The configurable port that RESTar handlers are registered to listen on. This is passed as an argument to RESTar.Config.Init() on start-up (see help/topic=config)
    
1.3. base_uri

The configurable base uri that RESTar handlers are registered to listen on. This is also passed as an argument to RESTar.Config.Init().
    
1.4. resource_locator

A case-insensitive string used for matching against available resources by name. Example: If no other resource has the name "Settings" The resource 'RESTar.Settings' can be located with resource locator 'settings'. If multiple resources are matched, a 409 response will be returned asking you to further qualify the resource locator.
    
1.5. conditions

Key-value pairs used for matching entities within a resource. For info about which methods utilize matching, see /help/topic=methods. Example: To get entities in a resource R with age of 35, GET: uri/R/age=35. For more examples, see help/topic=examples
    
1.6. meta-conditions

Conditions used to filter, order and limit the output of a command or otherwise configure the command itself. There are a set of predefined meta-conditions that can be used in commands – see help/topic=meta-conditions for more information.   

- - -

2. Resources

Resources are the most general of the objects that RESTar operates on. All tables (Starcounter database class definitions) decorated the RESTar attribute are declared as resources that RESTar can work with. But anything could be declared a resource  Resources are manipulated using methods. RESTar also declares some internal resources, for example 'settings' and 'help'.

Resources contain entities. The 'help' resource contains multiple 'help' entities (for example this article). All database objects belonging to a Starcounter database class declared as a RESTar resource (using the RESTar attribute) are resource entities.

- - -

3. Methods

RESTar enables interaction with its available resources through 5 methods, corresponding to 5 common HTTP verbs: GET, POST, PUT, PATCH and DELETE.

1.1. GET
    
Retreives entities from a resource that matches a set of conditions and applies a set of meta-conditions to the results. Returns 200 and a JSON formatted list of entities that match the given conditions.

Data: (no data)

1.2. POST

Inserts a JSON formatted object or a JSON formatted list of objects as entities into the specified resource without matching against existing entities. Returns 201 and a status message with a count of objects inserted on successfull insertion. 

Data: a JSON object or an array of JSON objects

1.3. PUT

Matches against an existing entity in the given resource, and either updates that entity or, if there is no entity in the resource matching the conditions, inserts a new entity. Returns 201 and the inserted object on successful insertion. If more than one entity is matched, returns 409.

Data: a JSON object

1.4. PATCH

Matches against an existing entity in the given resource, and updates the matched entity from JSON. Since PATCH will never generate new entities, the JSON can include values for only a subset of the resource's columns. PATCH will populate only with the data provided in the JSON, and leave the rest of the columns intact. Returns 200 on success and a status message with a count of objects updated.

There is an unsafe version of this method, see the 'Unsafe' section.

Data: a JSON object

1.5. DELETE

Matches against an existing entity in the given resource, and deletes that entity from the resource. Returns 

Data: (no data)

- - -

4. Safe and unsafe commands