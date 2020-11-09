set pgbinpath="C:\Program Files (x86)\PostgreSQL\9.6\bin"

set dumpfile="E:\08_Database\gap\teststraat_gap.dump"
rem teststraat database Eco2
set host=db-teststraat.pc.iesprd.ictu-sr.nl
set port=5432
set username=postgres
set owner=postgres
set dbname=teststraat

SET /P password=Password for user [%username%]:

%pgbinpath%\psql.exe -d postgresql://%host%/%dbname% -U %username% < %dumpfile%

