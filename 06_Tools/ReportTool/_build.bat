echo off

echo build...
dotnet build sources\ReportGeneratorTools.sln --configuration Release

echo publish...
dotnet publish sources\ReportGeneratorTools.sln --configuration Release

echo copy...
del /Q sources\build\*.*
copy sources\rpg.console\bin\Release\netcoreapp2.1\publish\*.* sources\build
copy sources\rpg.loadintermediate\bin\Release\netcoreapp2.1\publish\*.* sources\build
copy sources\rpg.merge\bin\Release\netcoreapp2.1\publish\*.* sources\build
copy sources\rpg.parsemeasures\bin\Release\netcoreapp2.1\publish\*.* sources\build
copy sources\rpg.parsetransactions\bin\Release\netcoreapp2.1\publish\*.* sources\build
copy sources\rpg.parsevariables\bin\Release\netcoreapp2.1\publish\*.* sources\build

echo done build
