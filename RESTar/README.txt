
# RESTar (alpha) - a REST Api for Starcounter
# RESTar is a work in progress, contact Erik at erik.von.krusenstierna@mopedo.com with questions, comments or issues
# See the example project for more concrete implementation details and guidelines
# More information is included in the help resource inside RESTar

# SECTIONS:
    1. Setup
    2 
    Examples

- - -

1. Setup

To get started using RESTar, make an assembly reference to the RESTar.dll file from your Visual Studio project, and add a 
using directive for the RESTar namespace to your Starcounter application. Then make a call to RESTar.RESTarConfig.Init() 
in your Starcounter application (for example in the constructor or Main method). When your application starts, RESTar will 
make its settings and resources available for you as Starcounter database entities. You can reach them by querying (using 
e.g. Starcounter Administrator): 'SELECT * FROM RESTar.Settings' or 'SELECT * FROM RESTar.Resources' respectively. By now 
RESTar will be fully up and running and you can reach the resources and send requests over its REST Api. The next step is 
to ensure that your resources are serializable in a way that works well with RESTar. See the RESTarExample project for a 
discussion on what this entails.  
 
When your RESTar instance is upp and running, make sure to check out the available help articles at GET /help






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