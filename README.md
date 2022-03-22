# SpaceTracker

Tracks the changes in a Revit project and saves the general structure of it in a realtional SQLite-Database and a graph-based Neo4j-Database to illustrate differences between those two types of DBs.

## Requirements

* Revit 2022

## Installation

1. Fork & clone the repo
2. add the `SpaceTracker/SpaceTracker.addin` file to the addins folder located at `C:\Users\$USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2022`
3. Make sure `Revit.exe` is set as an external program in the debug menu in Visual Studio (Poject --> Properties --> Debug)
4. Run, and an instance of Revit will open automatically