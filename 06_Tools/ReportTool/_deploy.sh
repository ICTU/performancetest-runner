#!/bin/bash

# deploy last built binaries to reportgenerator tools folder

echo deploy build to tools folder...
cp sources/build/*.* tools
echo get release notes...
cp sources/*.releasenotes.txt tools

echo done deploy
