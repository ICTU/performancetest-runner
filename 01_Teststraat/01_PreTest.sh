#!/bin/bash
echo "#######################################################################"
echo "Start Script '01_PreTest.sh' @ `date +"%T"`"
echo "-----------------------------------------------------------------------"

echo "Start with setting and checking incomming variables"

testtag=$1
runverification=$2

. functions.sh || aborttest "Could not include functions"
loadGlobals

test_variable "testtag" $testtag
test_variable "runverification" $runverification

echo "Done with setting and checking incomming variables"
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Get the project specific parameters

if [[ "$get_project_specific_parameters" == "true" ]]; then
 
	echo "Start with getting project specific parameters"
		. $projectfolder_root/$project/getparams.sh
	echo "Done with project specific parameters"

else
	echo "Getting project specific parameters is disabled"
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Get the script from SVN/GIT

if [[ "$getfromrepository" == "true" ]]; then
	echo "Start getting script from repository"
	update_repository $revision_control_tool
	echo "Done getting script from repository"
else
	echo "Get script from repository disabled"
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Running of the verification workload. This is done to:
# - Warmup the application
# - Validate if the application functionality is working as expected

if [[ "$runverification" == "true" ]]; then
	
	echo "Start with running verification run"	
	run_${tool} $scriptfolder $projectname $Vworkload $verification_logdir
	validation_check_${tool}
	echo "Done with running verification run"
else
	echo "Running Verification run is disabled"
fi
echo "-----------------------------------------------------------------------"
echo "End Script '01_PreTest.sh' @ `date +"%T"`"
echo "#######################################################################"