#!/bin/bash

# use of generic report wrapper:
# report_wrapper <projectcode> [jmeter|silk] <comment> <baseline>

rpt_projectname=$1
rpt_loadgen=$2
rpt_label=$3
rpt_baselineref=$4
rpt_partial=$5
#1=prepare 2=copy 3=parse 4=import 5=merge 6=copy (123=prepare..parse, 5=merge only, 123456 or ""=all)

echo [REPORT WRAPPER] READ LOCAL VARS...
. report_vars.incl

if [[ $rpt_partial == "" ]]; then rpt_partial="123456"; fi
echo [REPORT WRAPPER] PARTIAL=$rpt_partial

rpt_loglocation=$rpt_reporttargetpath
echo [REPORT WRAPPER] LOG LOCATION [$rpt_loglocation]

echo [REPORT WRAPPER] EXECUTE REPORT GENERATE...
. report_generate.sh > $rpt_loglocation/"$rpt_projectname"_report.log 2>&1

# code added in report_generate
echo [REPORT WRAPPER] DONE with exitcode $rpt_exitcode