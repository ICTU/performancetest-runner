echo off

echo ## phase 1: build...
call _build.bat

echo ## phase 2: deploy...
call _deploy.bat
