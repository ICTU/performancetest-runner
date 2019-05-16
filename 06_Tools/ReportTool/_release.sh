#!/bin/bash

# deploy last built binaries to reportgenerator tools folder

echo create folder structure...
rm -r -f release
mkdir release
mkdir release/tools

echo copy binaries...
cp tools/*.* release/tools
echo copy scripts...
cp report_*.sh release
echo copy release notes...

echo copy vars and global var example...
cp report_vars.incl release
cp ../../00_Globals/testautomation_globals.incl release/testautomation_globals.incl.example

echo done preparing release to folder 'release'
