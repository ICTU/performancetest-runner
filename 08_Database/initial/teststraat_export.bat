@echo off

set pgdumppath="C:\Program Files (x86)\PostgreSQL\9.3\bin"

set dumpfile=teststraat.dump
set dbname=teststraat
set username=postgres

echo Dump database [%dbname%] to [%dumpfile%]

rem No data, only structure/meta
%pgdumppath%\pg_dump.exe --schema-only --format=c --port=5432 --host=localhost --username=%username% --file=%dumpfile% %dbname%

rem With data
rem %pgdumppath%\pg_dump.exe --format=c --port=5432 --host=localhost --username=%username% --file=%dumpfile% %dbname%

echo Export is stored in file [%dumpfile%]