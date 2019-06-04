set pgbinpath="C:\Program Files (x86)\PostgreSQL\9.3\bin"

set dumpfile="teststraat.dump"
set host=localhost
set port=5432
set username=postgres
set owner=postgres
set dbname=teststraat

echo Drop current database %dbname% (if any)...
%pgbinpath%\dropdb  --host=%host% --port=%port% --username=%username% %dbname%

echo Create database %dbname%...
%pgbinpath%\createdb --host=%host% --port=%port% --username=%username% --owner=%owner% %dbname%

echo Restore dump of database %dbname%...
%pgbinpath%\pg_restore.exe --format=c --username=%username% --port=%port% --host=%host% --dbname=%dbname% --single-transaction --exit-on-error %dumpfile%