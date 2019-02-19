rem call _clean.bat
rem call _exportdb.bat

rd release /s /q
md release
md release\tools

xcopy tools release\tools /s /y
del release\tools\*.config
copy releases\teststraat.releasenotes.txt release

copy report_*.sh release

copy report_vars.incl release
copy ..\..\00_Globals\testautomation_globals.incl release\testautomation_globals.incl.example