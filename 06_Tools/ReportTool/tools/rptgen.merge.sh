#!/bin/bash

echo "Merge (data and html template) START"

echo runname=$rpt_runname
echo rptgendatetime=$rpt_rptgendatetime
echo reporttemplatefilename=$rpt_reporttemplatefilename
echo reporttemptargetfile=$rpt_reporttemptargetfile

#error handling
aborttest_merge() {
	echo "STOP, error in rptgen.merge ($1)"
	rpt_exitcode=1
	#exit 0
}

echo "GenerateReport - Arrange prerequisites..."
if [[ ! -e $rpt_reporttemplatefilename ]]; then aborttest "report template not found"; fi
cp -f $rpt_reporttemplatefilename $rpt_reporttemptargetfile
cp -f $rpt_reporttemplatefilename $rpt_temppath/report.00.html

echo "GenerateReport - Merge data with html template..."
cp -f $rpt_reporttemplatefilename $rpt_reporttemptargetfile
dotnet $rpt_toolspath/rpg.merge.dll i project=$rpt_projectname testrun=$rpt_runname category=msr entity=* templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge measurements/msr"; return; fi
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.01.html

dotnet $rpt_toolspath/rpg.merge.dll it project=$rpt_projectname testrun=$rpt_runname category=trs templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge transactiondata/trs"; return; fi
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.02.html

dotnet $rpt_toolspath/rpg.merge.dll ib project=$rpt_projectname testrun=$rpt_runname category=trs baselinetestrun=$rpt_baselineref templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge transaction baselinedata/trs"; return; fi
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.03.html

echo "GenerateReport - Merge variables with template..."
dotnet $rpt_toolspath/rpg.merge.dll i project=$rpt_projectname testrun=$rpt_runname category=var templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge variables/var"; return; fi
dotnet $rpt_toolspath/rpg.merge.dll t project=$rpt_projectname templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge threshold data"; return; fi
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=projectname value=$rpt_projectname templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=label "value=$rpt_label" templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=messages "value=$rpt_messages" templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=rptgendatetime value=$rpt_rptgendatetime templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=baselineref value=$rpt_baselineref templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=maxtrendcount value=$rpt_maxtrendcount templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=trendworkload value=$rpt_workload templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=companylogoreference value=$rpt_companylogoreference templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.04.html

echo "GenerateReport - Cleanup non-used variables top section of the report..."
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=* value=" " templatefile=$rpt_reporttemptargetfile beginpattern="Response times (ms)" endpattern="Trend (ms)" database=$rpt_reportdbconnectstring
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.05.html

echo "GenerateReport - Merge trend data (all transactions)..."
dotnet $rpt_toolspath/rpg.merge.dll jt project=$rpt_projectname category=trs valueindex=4 historycount=$rpt_maxtrendcount templatefile=$rpt_reporttemptargetfile workload=$rpt_workload database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge transaction trenddata"; return; fi
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.06.html 

echo "GenerateReport - Merge trend data (all variables)..."
dotnet $rpt_toolspath/rpg.merge.dll j project=$rpt_projectname category=var historycount=$rpt_maxtrendcount templatefile=$rpt_reporttemptargetfile workload=$rpt_workload database=$rpt_reportdbconnectstring
if [ $? -ne 0 ]; then aborttest_merge "merge variable trenddata"; return; fi
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.07.html

echo "GenerateReport - Cleanup non-used variables rest of the report..."
dotnet $rpt_toolspath/rpg.merge.dll v project=$rpt_projectname name=* value=" " templatefile=$rpt_reporttemptargetfile database=$rpt_reportdbconnectstring
cp -f $rpt_reporttemptargetfile $rpt_temppath/report.08.html

echo "Merge (data and html template) DONE"
