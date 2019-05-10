#!/bin/bash

echo "Report generate START"

rpt_messages=

echo projectname=$rpt_projectname
echo loadgen=$rpt_loadgen
echo label=$rpt_label
echo baselineref=$rpt_baselineref
echo temppath=$rpt_temppath
echo reporttargetfilebase=$rpt_reporttargetfilebase
echo reporterrorfile=$rpt_reporterrorfile
echo resultpath=$rpt_resultpath
echo partial=$rpt_partial

rpt_exitcodeprefix="$rpt_projectname"_report

#error handling
aborttest() {
	echo "abort script"
	echo $rpt_exitcodeprefix=$1 > $rpt_reporterrorfile
	exit 1
}

if [[ $rpt_partial == *"1"* ]]; then
	echo
	echo "* Phase 1: Arrange prerequisites"

	echo "clean reportgen temp dir..."
	rm -rf $rpt_temppath
	mkdir $rpt_temppath
fi

if [[ $rpt_partial == *"2"* ]]; then
	echo
	echo "* Phase 2: collect data ready to convert to intermediate format"
	
	# Copy testdata: transaction files...
	if [[ $rpt_loadgen == "silk" ]]; then

		if [[ ! -e $rpt_resultpath/baselineReport.brp ]]; then
			echo "exit reporting, testresult (.brp) not found at $rpt_resultpath"
			aborttest "2 (missing file) .brp"
		else
			echo "collect silk testresult (tsd + brp) from $rpt_resultpath..."

			# copy tsd (sorry for the mess)
			cd $rpt_resultpath
			cp m*.tsd $rpt_temppath
			cd $rpt_temppath
			cat m@*.tsd > _transactions.tsd

			# copy brp
			cp $rpt_resultpath/baselineReport.brp $rpt_temppath/_transactions.brp
		fi
	fi

	if [[ $rpt_loadgen == "jmeter" ]]; then

		if [[ ! -e $rpt_resultpath/result.jtl ]]; then
			echo "exit reporting, testresult (.jtl) not found at $rpt_resultpath"
			aborttest "2 (missing file) jtl"
		else
			echo "collect jmeter result (jtl) from $rpt_resultpath..."
			cp $rpt_resultpath/result.jtl $rpt_temppath/_transactions.jtl
		fi	
	fi

	# Copy testdata: runinfo files...
	if [[ ! -e $rpt_runinfopath/runinfo.txt ]]; then
		echo "exit reporting, runinfo file not found at $rpt_runinfopath"
		aborttest "2 (missing file) runinfo"
	else
		echo "collect runinfo from $rpt_runinfopath..."
		cp $rpt_runinfopath/runinfo.txt $rpt_temppath/_intermediate.var.runinfo.csv
	fi
fi

if [[ $rpt_partial == *"3"* ]]; then
  echo
  echo "* Phase 3: convert and parse for $rpt_loadgen"
  . $rpt_toolspath/rptgen.parse."$rpt_loadgen".sh
  if [ $? -ne 0 ]; then aborttest "3 (parse error)"; fi
fi

if [[ $rpt_partial == *"4"* ]]; then
  echo
  echo "* Phase 4: import parsed data"
  . $rpt_toolspath/rptgen.import.sh
  if [ $? -ne 0 ]; then aborttest "4 (import error)"; fi
fi

if [[ $rpt_partial == *"5"* ]]; then
  echo
  echo "* Phase 5: Merge"
  . $rpt_toolspath/rptgen.merge.sh
  if [ $? -ne 0 ]; then aborttest "5 (merge error)"; fi
fi

if [[ $rpt_partial == *"6"* ]]; then
  echo
  echo "* Phase 6: Copy result report"
  . $rpt_toolspath/rptgen.copytotarget.sh
  if [ $? -ne 0 ]; then aborttest "6 (copy to target error)"; fi
fi

#wrap up, success
echo "$rpt_exitcodeprefix=0 (success)" > $rpt_reporterrorfile

echo "Report generate DONE"