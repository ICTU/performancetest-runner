#!/bin/bash

echo "Merge (data and html template) START"

echo runname=$rpt_runname
echo rptgendatetime=$rpt_rptgendatetime
echo reporttemplatefilename=$rpt_reporttemplatefilename
echo reporttemptargetfile=$rpt_reporttemptargetfile

#error handling
aborttest() {
	echo "STOP, error in rptgen.merge ($1)"
	exit 1
}

echo "GenerateReport - Arrange prerequisites..."
if [[ ! -e $rpt_reporttemplatefilename ]]; then aborttest "report template not found"; fi
cp $rpt_reporttemplatefilename $rpt_reporttemptargetfile
cp $rpt_reporttemplatefilename $rpt_temppath/report.00.html

echo "GenerateReport - Merge data with html template..."
cp $rpt_reporttemplatefilename $rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe i project=$rpt_projectname testrun=$rpt_runname category=msr entity=* templatefile=$rpt_reporttemptargetfile
if [ $? -ne 0 ]; then aborttest "merge measurements/msr"; fi
cp $rpt_reporttemptargetfile $rpt_temppath/report.01.html

$rpt_toolspath/srt.merge.exe it project=$rpt_projectname testrun=$rpt_runname category=trs templatefile=$rpt_reporttemptargetfile
if [ $? -ne 0 ]; then aborttest "merge transactiondata/trs"; fi
cp $rpt_reporttemptargetfile $rpt_temppath/report.02.html

$rpt_toolspath/srt.merge.exe ib project=$rpt_projectname testrun=$rpt_runname category=trs baselinetestrun=$rpt_baselineref templatefile=$rpt_reporttemptargetfile
if [ $? -ne 0 ]; then aborttest "merge transaction baselinedata/trs"; fi
cp $rpt_reporttemptargetfile $rpt_temppath/report.03.html

echo "GenerateReport - Merge variables with template..."
$rpt_toolspath/srt.merge.exe i project=$rpt_projectname testrun=$rpt_runname category=var templatefile=$rpt_reporttemptargetfile
if [ $? -ne 0 ]; then aborttest "merge variables/var"; fi
$rpt_toolspath/srt.merge.exe t project=$rpt_projectname templatefile=$rpt_reporttemptargetfile
if [ $? -ne 0 ]; then aborttest "merge threshold data"; fi
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=projectname value=$rpt_projectname templatefile=$rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=label "value=$rpt_label" templatefile=$rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=messages "value=$rpt_messages" templatefile=$rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=rptgendatetime value=$rpt_rptgendatetime templatefile=$rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=baselineref value=$rpt_baselineref templatefile=$rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=maxtrendcount value=$rpt_maxtrendcount templatefile=$rpt_reporttemptargetfile
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=trendworkload value=$rpt_workload templatefile=$rpt_reporttemptargetfile
cp $rpt_reporttemptargetfile $rpt_temppath/report.04.html

echo "GenerateReport - Cleanup non-used variables top section of the report..."
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=* value=" " templatefile=$rpt_reporttemptargetfile beginpattern="Response times (ms)" endpattern="Trend (ms)"
cp $rpt_reporttemptargetfile $rpt_temppath/report.05.html

echo "GenerateReport - Merge trend data (all transactions)..."
$rpt_toolspath/srt.merge jt project=$rpt_projectname category=trs valueindex=4 historycount=$rpt_maxtrendcount templatefile=$rpt_reporttemptargetfile workload=$rpt_workload
if [ $? -ne 0 ]; then aborttest "merge transaction trenddata"; fi
cp $rpt_reporttemptargetfile $rpt_temppath/report.06.html 

echo "GenerateReport - Merge trend data (all variables)..."
$rpt_toolspath/srt.merge j project=$rpt_projectname category=var historycount=$rpt_maxtrendcount templatefile=$rpt_reporttemptargetfile workload=$rpt_workload
if [ $? -ne 0 ]; then aborttest "merge variable trenddata"; fi
cp $rpt_reporttemptargetfile $rpt_temppath/report.07.html

echo "GenerateReport - Cleanup non-used variables rest of the report..."
$rpt_toolspath/srt.merge.exe v project=$rpt_projectname name=* value=" " templatefile=$rpt_reporttemptargetfile
cp $rpt_reporttemptargetfile $rpt_temppath/report.08.html

echo "Merge (data and html template) DONE"
