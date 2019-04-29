#!/bin/bash
. ..\\..\\00_Globals\\testautomation_globals.incl
dotnet ./tools/rpg.console.dll $1 database=$reportdbconnectstring
