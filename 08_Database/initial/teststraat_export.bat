set pgdumppath="C:\Program Files (x86)\PostgreSQL\9.3\bin"

set dumpfile="teststraat.dump"
set dbname=teststraat

echo Dump database %dbname% to %dumpfile%
%pgdumppath%\pg_dump.exe --format=c --port=5432 --host=localhost --username=postgres --file=%dumpfile% %dbname%