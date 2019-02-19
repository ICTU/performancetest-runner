#!/bin/bash

echo "Copy to target (copy report to target) START"

rpt_reporttargetfilestandard="$rpt_reporttargetfilebase"_"$rpt_workload"_"$rpt_runname".html
rpt_reporttargetfilehistory="$rpt_reporttargetfilebase"_"$rpt_workload".html
rpt_reporttargetfileold="$rpt_reporttargetfilebase"_report.html
rpt_reporttargetfileindex=$rpt_reporttargetpath/index.html

echo reporttargetfilestandard=$rpt_reporttargetfilestandard
#echo rpt_reporttargetfilehistory=$rpt_reporttargetfilehistory
#echo rpt_reporttargetfileold=$rpt_reporttargetfileold
#echo rpt_reporttargetfileindex=$rpt_reporttargetfileindex

echo "Copy html report to target directory $rpt_reporttargetpath..."
cp $rpt_reporttemptargetfile $rpt_reporttargetfilestandard
cp $rpt_reporttemptargetfile $rpt_reporttargetfilehistory
cp $rpt_reporttemptargetfile $rpt_reporttargetfileold
cp $rpt_reporttemptargetfile $rpt_reporttargetfileindex

echo "Copy static resources to target directory $rpt_reporttargetpath\\js..."
rm -r $rpt_reporttargetpath\\js
mkdir $rpt_reporttargetpath\\js
cp -r $rpt_toolspath/js $rpt_reporttargetpath

echo
echo "Copy to target DONE"
