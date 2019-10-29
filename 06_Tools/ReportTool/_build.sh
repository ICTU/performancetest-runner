#!/bin/bash

# build new binaries from sources

echo build...
dotnet build sources/ReportGeneratorTools.sln --configuration Release

echo publish...
dotnet publish sources/ReportGeneratorTools.sln --configuration Release

echo create build folder...
mkdir -p sources/build


echo copy...
rm -f sources/build/*.*
cp sources/rpg.console/bin/Release/netcoreapp2.1/publish/*.* sources/build
cp sources/rpg.loadintermediate/bin/Release/netcoreapp2.1/publish/*.* sources/build
cp sources/rpg.merge/bin/Release/netcoreapp2.1/publish/*.* sources/build
cp sources/rpg.parsemeasures/bin/Release/netcoreapp2.1/publish/*.* sources/build
cp sources/rpg.parsetransactions/bin/Release/netcoreapp2.1/publish/*.* sources/build
cp sources/rpg.parsevariables/bin/Release/netcoreapp2.1/publish/*.* sources/build

echo done build
