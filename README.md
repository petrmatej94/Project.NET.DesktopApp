# Project for subject Architecture of the .NET Technology  

Windows Desktop Application which downloads forex data from API and convert them into Chart. Each forex pair is represented as alone DLL library. At first, you have to create library with LibraryBuilder, then import .dll file into CurrencyVisualization project. Plugins are loaded dynamicaly, they can be plugged in as app is running.

## Requirements
- Application will be extensible by DLL plugins - they will be loaded dynamicaly as application runs
- Plugin will be checked if it implements required interface
- Application will save/load data to/from XML file
- Application will be using Debug & Trace logging
- Custom Exceptions, Events, Lambda functions, Threads
