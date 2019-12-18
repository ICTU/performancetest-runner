#!/bin/bash
echo "||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||"
echo "-----------------------------------------------------------------------"
echo "Start script $0 on `date +"%m-%d-%y"` @ `date +"%T"`"
echo "-----------------------------------------------------------------------"

. ./functions.sh

buildnumber=$1
prodload=$2
baseline=$3
project=$4
runverification=$5

numberofvars=5

echo "-----------------------------------------------------------------------"
echo "The script received $# arguments"
echo "The script expected "$numberofvars" arguments"

if [[ "$#" != $numberofvars ]]; then
	echo "Number of arguments does not match, aborting..."
	exit 1
else
	echo "The number of arguments supplied is correct continuing..."
	
	test_variable "buildnumber" $buildnumber
	test_variable "prodload" $prodload
	test_variable "baseline" $baseline
	test_variable "project" $project
	test_variable "verificatie" $runverification

	echo "All variables present and filled"
fi
echo "Done with setting and checking incomming variables"
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
echo "Start with creating and loading of globals"

createGlobals
loadGlobals

echo "Done with creating and loading of globals"
echo "-----------------------------------------------------------------------"	

echo
echo "-----------------------------------------------------------------------"
# testtag aanmaken
echo "Start creating testtag"
testtag="`date +"%y-%m-%d_%H_%M_%S"`_$project"
echo "testtag: $testtag"
echo "Done creating testtag"
echo "-----------------------------------------------------------------------"

echo
echo "-----------------------------------------------------------------------"
echo "Start writing start time + project to file"
echo "BuildlogLocation: $BuildlogLocation"
echo start=`date +"%y-%m-%d_%H_%M_%S"` : $project $prodload >> $BuildlogLocation
echo "Done writing start time + project to file"
echo "-----------------------------------------------------------------------"


echo
echo "-----------------------------------------------------------------------"
# ProjectOmgeving specifieke variabelen ophalen
echo "Start getting project specific variables"
if isdirectory $projectfolder_root/$project; then echo "Project specific folder exists"; else aborttest "Project unknown: $project"; fi 
if isfile $projectfolder_root/$project/vars.incl; then echo "Found project specific variables"; else aborttest "Project specific variables not found, aborting"; fi 
. $projectfolder_root/$project/vars.incl
echo "Done getting project specific variables"
echo "-----------------------------------------------------------------------"

echo
echo "-----------------------------------------------------------------------"
# Printen welke onderdelen van de test aanstaan

if [[ "$printoptions" == "true" ]]; then
	echo "Start printing enabled/disabled options"
	echo "---------------------------------------------------"
	echo "Teststraat variabelen"
	echo "-----"
	echo "Aantal verwachte variabelen:              : $numberofvars"
	echo "Locatie Silk Performer:                   : $loadgendir_silk"
	echo "Locatie JMeter:                           : $loadgendir_jmeter"
	echo "Buildlog locatie:                         : $BuildlogLocation"
	echo "Teststraat directory:                     : $testautomation_root"
	echo "Scriptfolder:                             : $scriptfolder"
	echo "Pad van project:                          : $scripts_root"
	echo "Project folder root                       : $projectfolder_root"	
	echo "Verificatie log directory:                : $verification_logdir"
	echo "Productie log directory:                  : $loadtest_logdir"
	echo "Log directory:                            : $logdir_root"
	echo "Log backup directory:                     : $logbackupdir"
	echo "Report tool directory:                    : $reporttoolfolder"
	echo "Used space threshold C in MB:             : $freespaceCthreshold"
	echo "Used space threshold E in MB:             : $freespaceEthreshold"
	echo 
	echo "Project specifieke variabelen"
	echo "-----"
	echo "Remove folders from previous test?        : $removelogfolders"
	echo "Check free disk space?                    : $checkdiskspace"
	echo "Run verification run?                     : $runverification"
	echo "Abort test if verification failed?        : $abortifverifyfailed"
	echo "Run main test?                            : $runmaintest"
	echo "Generate report?                          : $generatereport"
	echo "Gekozen workload?                         : $prodload"
	echo "Kill tool used for testing?               : $kill_project_tool"
	echo "rebuild destroyed directories?            : $rebuild_directories"
	echo "Get project specific parameters?          : $get_project_specific_parameters"
	echo "Remove old script folder?                 : $removescriptfolder"
	echo "Create a run log?                         : $create_run_log"
	echo "Run project specific post test script?    : $run_posttest"
	echo "Project naam:                             : $projectname"
	echo "Tool:                                     : $tool"
	echo "Verificatie workload:                     : $Vworkload"
	echo "Threshold verificatie in seconden:        : $threshold_Verificatie"
	echo "Threshold productie in seconden:          : $threshold_productie"
	echo "Threshold duurtest in seconden:           : $threshold_duurtest"
	echo "Commit to repository:                     : $committorepository"
	echo "Revision control tool:                    : $revision_control_tool"
	echo "Repository log directory:                 : $repository_log_drive"
	echo "Repository report directory:              : $repository_report_dir"	
	echo "---------------------------------------------------"
	echo "Finished printing options"
else
	echo "Printing of options is disabled"
fi

if [[ "$cleanbackupfolder" == "true" ]]; then
	
	cleanbackupfolder
	
fi
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Check the available disk space, abort if not enough available
# Will check space for each value in the hashmap with the provided space threshold
declare -A spaceHashTable=$spaceHashTableValues
declare -A spaceHashTableWarning=$spaceHashTableValuesWarning
for location in "${!spaceHashTable[@]}"; do 
	validateDiskSpace "$location" "${spaceHashTable[$location]}" "${spaceHashTableWarning[$location]}" 
done
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# each project folder has it's own script that allows for project specific checks that
# need to be performd before starting the test
if [[ "$project_specific_checks" == "true" ]]; then
	
	echo "Start with project specific checks"
		. $projectfolder_root/$project/checks.sh
	echo "Done with project specific checks"

else
	echo "Project specific checks disabled"
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Stoppen van tooling, voorkomt mogelijke verstoring (bijv: Silk kan niet starten als Silk al draait)

if [[ "$kill_project_tool" == "true" ]]; then
	#Hier eigenlijk nog een check of variabele is ingevuld
	kill_${tool}	
else
	echo "Killing kill_${tool} disabled" #liever niet op false zetten, is een risico 
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Removing of old folders (general)
if [[ "$removelogfolders" == "true" ]]; then
 
	# Cleaning previous logs
	echo "Start removing result and logging folders"
	rm -f -r "$logdir_root/"
	echo "Done removing result and logging folders"

else
	echo "Removing of log folders is disabled"
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Recreating folders & checking if they exist

if [[ "$rebuild_directories" == "true" ]]; then
 
	echo "Start with rebuilding directories"
		
		mkdir -p "$logbackupdir/$testtag"
		if isdirectory $logbackupdir/$testtag; then echo "$logbackupdir/$testtag created"; else aborttest "Could not create \"$logbackupdir/$testtag\" abort test"; fi 		
		
		mkdir -p "$loadtest_measures"	
		if isdirectory $loadtest_measures; then echo "$loadtest_measures created"; else aborttest "Could not create \"$loadtest_measures\" abort test"; fi
		
	echo "Done with rebuilding directories"

else
	echo "Rebuilding directories is disabled"
fi

echo "-----------------------------------------------------------------------"
. ./01_PreTest.sh $testtag $runverification || aborttest
cd $testautomation_root
. ./02_PerfTest.sh $testtag $prodload || aborttest
cd $testautomation_root
. ./03_PostTest.sh $testtag $prodload || aborttest
cd $testautomation_root
. ./04_GenerateReport.sh $testtag $prodload $baseline || aborttest

echo "-----------------------------------------------------------------------"
echo "Start writing stop time + project to file"
echo stop=`date +"%y-%m-%d_%H_%M_%S"` : $project $prodload >> $BuildlogLocation
echo "Done writing stop time + project to file"
echo "-----------------------------------------------------------------------"

echo "End script $0 on `date +"%m-%d-%y"` @ `date +"%T"`"
echo "-----------------------------------------------------------------------"
echo "||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||"

