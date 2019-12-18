#!/bin/bash
echo "#######################################################################"
echo "Start Script '02_PerfTest.sh' @ `date +"%T"`"
echo "-----------------------------------------------------------------------"

echo "Start with setting and checking incomming variables"

testtag=$1
Pworkload=$2

. functions.sh || aborttest "Could not include functions"
loadGlobals

test_variable "testtag" $testtag
test_variable "Pworkload" $Pworkload

echo "Done with setting and checking incomming variables"
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"

if [[ "$startstopnmon" == "true" ]]; then

	## Start NMon
	echo "Start starting NMon"

	cygstart ${testautomation_root}/tools/NMon/startlogging.bat

	echo "Done starting NMon.."
	
else
	echo "Gather NMon metrics is disabled"
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Running the actual test
if [[ "$runmaintest" == "true" ]]; then
	
	echo "#########"
	echo "Start $Pworkload run"

	sleep 30
	# Start
	run_${tool} $scriptfolder $projectname $Pworkload $loadtest_logdir
	
echo "-----------------------------------------------------------------------"

	cd $testautomation_root
	
	echo "Done with Performance run"
	echo "#########"
	
	# Check if we have test results
	succescheck_${tool}
	
else
	echo "Run Production disabled"
fi

echo "-----------------------------------------------------------------------"

echo "Results found, continuing with the post test..."

echo "-----------------------------------------------------------------------"
echo "-----------------------------------------------------------------------"	
echo "End Script '02_PerfTest.sh' @ `date +"%T"`"
echo "#######################################################################"