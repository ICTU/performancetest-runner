#!/bin/bash
echo "#######################################################################"
echo "Start Script '03_PostTest.sh' @ `date +"%T"`"
echo "-----------------------------------------------------------------------"
echo "Start with setting and checking incomming variables"

testtag=$1
prodload=$2
taglabel=$3

. functions.sh || aborttest "Could not include functions"
loadGlobals

test_variable "testtag" $testtag
test_variable "prodload" $prodload

echo "-----------------------------------------------------------------------"

echo "Done with setting and checking incomming variables"
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Run the project specific post test script
if [[ "$run_posttest" == "true" ]]; then
	
	echo  "Start with project specific posttest tasks"
	. $projectfolder_root/$project/posttest.sh $testtag
	echo  "Done with project specific posttest tasks"
	
else
	echo "Running of project specific post test actions is disabled"
fi
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Creation of runinfo file (Required for ReportTool)
if [[ "$create_run_log" == "true" ]]; then
	
	echo "Start with creation of runinfo"
	echo svntag=$taglabel > "$logdir_root/runinfo.txt"
	echo "Done with creation of runinfo"
	
	echo "Add workload to runinfo"
	echo workload=$prodload >> "$logdir_root/runinfo.txt"
	echo "Done with adding workload to runinfo"
	
	echo "Add application version to runinfo"
	echo $applicatieversie
	if [[ -n $applicatieversie ]]; then
		echo appversion=$applicatieversie >> "$logdir_root/runinfo.txt"
	else
		echo appversion="" >> "$logdir_root/runinfo.txt"
	fi
	echo "Done with adding application version to runinfo"

else
	echo "Create runinfo log is disabled"
fi
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Moving logs
if [[ "$movelogs" == "true" ]]; then
	echo "Start copying result logs for backup"

	movelogs_src=$loadtest_logdir
	movelogs_dst=$logbackupdir/$testtag/testresults
	
	echo source: $movelogs_src
	echo destination: $movelogs_dst
	
	mkdir -p $movelogs_dst
	cp -f -r $movelogs_src/. $movelogs_dst/
	
	echo "Done copying logs for backup"
else
	echo "Moving logs disabled"
fi
echo "-----------------------------------------------------------------------"
echo
echo "End Script '03_PostTest.sh' @ `date +"%T"`"
echo "#######################################################################"