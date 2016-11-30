
# RESTar (alpha) - an audacious REST Api for Starcounter
# This readme will be included as a resource in RESTar in a future version
# RESTar is a work in progress, contact Erik at erik.von.krusenstierna@mopedo.com with questions, comments or issues.

# SECTIONS:
    1. Commands
    2. Resources
    3. Methods
    4. Safe and unsafe commands
    5. Examples

1. Commands
    Commands are syntactic units used as input to RESTar in order to invoke methods and interact with resources. Commands have the following syntactic structure:
    http://[ip_address][:port]/[uri][/resource_locator][/conditions][/meta-conditions]

    ip_address: the IP address used to reach your Starcounter application.
    
    port: the configurable port that RESTar handlers are registered to listen on. This is passed as an argument to RESTar.Config.Init() on start-up (see help/topic=config)
    
    uri: the configurable base uri that RESTar handlers are registered to listen on. This is also passed as an argument to RESTar.Config.Init().
    
    resource_locator: a case-insensitive string used for matching against available resources by name. Example: If no other resource has the name "Settings" The resource 'RESTar.Settings' can be located with resource locator 'settings'. If multiple resources are matched, a 409 response will be returned asking you to further qualify the resource locator.
    
    conditions: key-value pairs used for matching entities within a resource. For info about which methods utilize matching, see /help/topic=methods. Example: To get entities in a resource R with age of 35, GET: uri/R/age=35. For more examples, see help/topic=examples
    
    meta-conditions: conditions used to filter, order and limit the output of a command or otherwise configure the command itself. There are a set of predefined meta-conditions that can be used in commands – see help/topic=meta-conditions for more information.   

2. Resources
    Resources are the most basic of the main objects that RESTar operates on. All tables (Starcounter database class definitions) decorated the RESTar attribute are declared as resources that RESTar can operate on. Resources are manipulated using methods (see help/topic=methods). RESTar also declares some internal resources, for example 'settings' and 'help'. These are also Starcounter database classes, but they are declared within the RESTar assembly. 
    Resources contain entities. The 'help' resource contains multiple 'help' entities (for example this article). The 'settings' resource, however, contains only one entity. All database objects belonging to a Starcounter database class declared as a RESTar resource (using the RESTar attribute) are resource entities.

3. Methods
    RESTar enables interaction with its available resources through 5 methods, corresponding to 5 common HTTP verbs: GET, POST, PUT, PATCH and DELETE.

    GET
        Does: Retreives entities from a resource that matches a set of conditions and applies a set of meta-conditions to the results. I 
        


        GET will return a JSON formatted list of entities that match the given conditions.
    
    POST: POST inserts a JSON formatted object or a JSON formatted list of objects as entities into the specified resource without matching against existing entities.
    PUT: 
    PATCH:
    DELETE:

4. Safe and unsafe commands