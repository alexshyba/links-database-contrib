Sitecore Links Database Contrib
======================

Small research project with tweaks to Sitecore Links Database.

##What is included?

###Optimization to Links maintenance
The links update is triggered via the following events:
* item:saved
* item:deleted
* item:versionRemoved

This project patches the implementation of these event handlers with an alternative way of connecting to Links Database.
This approach proved to be helpful for some installations where such operations as item save were either extremely slowed down or terminated with a SQL timeout on Links Database.
In addition, the code offloads the UI thread and performs the update in a background job (separate thread).

###FrontEndLinkDatabase
This component approaches the problem of shared links database in a two server environment (separate CM and CD).
In addition to keeping a separate reference to the front-end links db, the project ships with the following event handlers that ensure that front-end links database is maintained on during publishing:
*publish:itemProcessing
*publish:itemProcessed

Read more:
http://sitecoreblog.alexshyba.com/2010/09/optimizing-sitecore-link-database.html

##Issues?
Please submit the ticket here:
https://github.com/sitecorian/links-database-contrib/issues
