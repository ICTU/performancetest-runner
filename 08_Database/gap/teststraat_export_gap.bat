@echo off

set pgdumppath="C:\Program Files (x86)\PostgreSQL\9.6\bin"

set host=db.db-teststraat.performance.ictu
set dumpfile=teststraat_gap.dump
set dbname=teststraat
set username=postgres

echo Dump database [%dbname%] to [%dumpfile%]

rem With data
%pgdumppath%\pg_dump.exe --data-only --format=p --column-inserts --exclude-table=threshold --port=5432 --host=%host% --username=%username% --file=%dumpfile% %dbname% 
echo Export is stored in file [%dumpfile%]