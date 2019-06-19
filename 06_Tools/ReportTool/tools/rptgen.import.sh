#!/bin/bash

echo "Import (of intermediate data) START"

echo projectname=$rpt_projectname
echo temppath=$rpt_temppath
echo toolspath=$rpt_toolspath

#error handling
aborttest_import() {
	echo "STOP, error in rptgen.import ($1)"
	rpt_exitcode=1
	# exit 0
}

echo
echo "Extract variables from intermediate key-value pairs..."
source $rpt_temppath/_intermediate.var.csv
rpt_rptgendatetime=$(echo "$rptgendatetime0" | tr -d '\n' | tr -d '\r')
rpt_runname=$(echo "$testrundatetime" | tr -d '\n' | tr -d '\r') # dit verwijdert een ingeslopen \r of \n - #wateendrama, was alleen nodig voor runname

source $rpt_temppath/_intermediate.var.runinfo.csv # read intermediate key-value pairs
rpt_workload=$(echo "$workload" | tr -d '\n' | tr -d '\r')

echo runname=$rpt_runname
echo rptgendatetime=$rpt_rptgendatetime
echo workload=$rpt_workload

echo
echo Import intermediate data into database...
# measure data + vars from measure data
dotnet $rpt_toolspath/rpg.loadintermediate.dll project=$rpt_projectname testrun=$rpt_runname category=msr entity=perf intermediatefile=$rpt_temppath/_intermediate.msr.csv database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_import "load measurements"; return; fi
dotnet $rpt_toolspath/rpg.loadintermediate.dll project=$rpt_projectname testrun=$rpt_runname category=var entity=msr intermediatefile=$rpt_temppath/_intermediate.var.msr.csv database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_import "load transactions"; return; fi
# transaction data + vars from measure data
dotnet $rpt_toolspath/rpg.loadintermediate.dll project=$rpt_projectname testrun=$rpt_runname category=trs entity=- intermediatefile=$rpt_temppath/_intermediate.trs.csv database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_import "load transactions"; return; fi
dotnet $rpt_toolspath/rpg.loadintermediate.dll project=$rpt_projectname testrun=$rpt_runname category=var entity=trs intermediatefile=$rpt_temppath/_intermediate.var.trs.csv database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_import "load transactions"; return; fi
# variables from data
dotnet $rpt_toolspath/rpg.loadintermediate.dll project=$rpt_projectname testrun=$rpt_runname category=var entity=generic intermediatefile=$rpt_temppath/_intermediate.var.csv database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_import "load variables global"; return; fi
# runinfo (read directly from teststraat)
dotnet $rpt_toolspath/rpg.loadintermediate.dll project=$rpt_projectname testrun=$rpt_runname category=var entity=runinfo intermediatefile=$rpt_temppath/_intermediate.var.runinfo.csv database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_import "load variables runinfo"; return; fi

echo "Import (of intermediate data) DONE"
