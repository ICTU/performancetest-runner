#!/bin/bash

# build and deploy

echo ## phase 1: build...
. _build.sh

echo ## phase 2: deploy...
. _deploy.sh
