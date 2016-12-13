
# RESTar (alpha) - a REST Api for Starcounter
# RESTar is a work in progress, contact Erik at erik.von.krusenstierna@mopedo.com with questions, comments or issues
# See the example project for more concrete implementation details and guidelines
# More information is included in the help resource inside RESTar

Setup

To get started using RESTar, make an assembly reference to the RESTar.dll file from your Visual Studio project, and add a 
using directive for the RESTar namespace to your Starcounter application. Then make a call to RESTar.RESTarConfig.Init() 
in your Starcounter application (for example in the constructor or Main method). When your application starts, RESTar will 
make its settings and resources available for you as Starcounter database entities. You can reach them by querying (using 
e.g. Starcounter Administrator): 'SELECT * FROM RESTar.Settings' or 'SELECT * FROM RESTar.Resources' respectively. By now 
RESTar will be fully up and running and you can reach the resources and send requests over its REST Api. The next step is 
to ensure that your resources are serializable in a way that works well with RESTar. See the RESTarExample project for a 
discussion on what this entails.  
 
When your RESTar instance is upp and running, make sure to check out the available help articles at GET /help. To include
whitespace in a URI, necessary for individual access to articles with topics like 'data types', URI component encode the
condition value including the whitespace, e.g. restar/help/topic=data%20types .