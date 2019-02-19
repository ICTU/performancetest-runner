#!/bin/bash

# use of generic report wrapper:
# report_wrapper <projectcode> [jmeter|silk] <comment> <baseline>

rpt_projectname=$1
rpt_loadgen=$2
rpt_label=$3
rpt_baselineref=$4

echo [REPORT WRAPPER] READ LOCAL VARS...
. report_vars.incl

rpt_loglocation=$rpt_reporttargetpath
echo [REPORT WRAPPER] LOG LOCATION [$rpt_loglocation]

echo [REPORT WRAPPER] EXECUTE REPORT GENERATE...
. report_generate.sh > $rpt_loglocation\\"$rpt_projectname"_report.log 2>&1
