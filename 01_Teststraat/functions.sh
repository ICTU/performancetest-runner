#!/bin/bash

# function to end script
aborttest() {
	echo $1
	exit 1
}

createGlobals() {
	
	# Check if globals_location is known
	# Load globals_loaction
	if isfile ./testautomation_globals_location.incl; then 
		echo "Found location of test automation global variables, including it now..."
		. ./testautomation_globals_location.incl
	else
		aborttest "ERROR: Could not find \"testautomation_globals_location.incl\". Should be present in the performancetest-runner root. Probably something wrong with setup, aborting..."
	fi
	
	defaults_location="./template/testautomation_globals_defaults.incl"
	lastline_default=$(tail -1 $defaults_location)
	if [[ "$lastline_default" != "# Dummyline for sed" ]]; then
		echo -e "\\n# Dummyline for sed" >> $defaults_location
		lastline_default=$(tail -1 $defaults_location)
		if [[ "$lastline_default" != "# Dummyline for sed" ]]; then
			echo "*****************************"
			echo "ERROR" 
			echo "Could not add dummy line to $defaults_location"
			echo "Please contact support!"
			echo "*****************************"
			aborttest "Aborting test..."
		fi
	fi	
	
	overrides_location="$testautomation_globals_location/testautomation_globals_overrides.incl"
	if isfile $overrides_location; then
		
		# Check if there is an empty line at the end of the overrides, this is required for sed to evaluate
		lastline_overwrite=$(tail -1 $overrides_location)
		if [[ "$lastline_overwrite" != "# Dummyline for sed" ]]; then
			echo -e "\\n# Dummyline for sed" >> $overrides_location
			lastline_overwrite=$(tail -1 $overrides_location)
			if [[ "$lastline_overwrite" != "# Dummyline for sed" ]]; then
				echo "*****************************"
				echo "ERROR" 
				echo "Could not add dummy line to $overrides_location"
				echo "Please contact support!"
				echo "*****************************"
				aborttest "Aborting test..."
			fi
		fi
		
		# Check if all required variables are filled
		numberOfValuesToFill=$(grep "ENTER_VALUE" $overrides_location | wc -l)
		if [[ $numberOfValuesToFill -gt 0 ]]; then
			echo "*****************************"
			echo "ERROR" 
			echo "There are still variables containing [ENTER_VALUE] that require a value"
			echo "Location: $overrides_location"
			echo "Aborting test"
			echo "*****************************"
			exit 1
		else
			echo "All overwrite variables contain a value, merging with defaults..."
			MergeGlobals
			numberOfValuesToFill=$(grep "ENTER_VALUE" $testautomation_globals_location/testautomation_globals.incl | wc -l)
			if [[ $numberOfValuesToFill -gt 0 ]]; then
				echo "*****************************"
				echo "ERROR" 
				echo "Merge was not fully successfull there are still variables containing [ENTER_VALUE] that require a value"
				echo "Location: $testautomation_globals_location/testautomation_globals.incl"
				echo "Aborting test"
				echo "*****************************"
				exit 1
			fi
		fi
	else
		echo "*****************************"
		echo "ERROR"
		echo "No overrides file present yet, creating it now in $testautomation_globals_location" 
		cp ./template/testautomation_globals_overrides.incl $testautomation_globals_location
		echo "Created testautomation_globals_overrides.incl file in $testautomation_globals_location please fill it with the correct values before rerunning the test!"
		echo "*****************************"
		aborttest "Stopping test now..."
	fi
		
}

MergeGlobals() {
	defaults_location="./template/testautomation_globals_defaults.incl"
	overrides_location="$testautomation_globals_location/testautomation_globals_overrides.incl"
	output_location="$testautomation_globals_location/testautomation_globals.incl"

	echo "--Start MergeGlobals--"
	echo "---Start Part 1---"
	echo "Put all the key=value pairs from the defaults file into the defaultsArray"
	declare -A defaultsArray=( )   # Initialize an empty array for the defaults
	while read line; do
		if [[ $line == *"="* ]]; then 				   # only do lines containing = should be used
	  		key=$(echo $line | cut -d '=' -f1) 		   # everything on the left of =
			value=$(echo $line | cut -d '=' -f2-) 	   # everything on the right of =
			defaultsArray[$key]=$value				   # store the variable name as key, store the variable name as value
			# echo "key: $key | value: $value"	
		fi
	done < $defaults_location
	declare defaultsArray       # print resulting array
	echo "---End Part 1---"
	
	echo
	
	echo "---Start Part 2---"
	echo "Grab all the key=value pairs from the override file into the overridesArray"
	declare -A overridesArray=( )   # Initialize an empty array for the overrides
	while read line; do
		if [[ $line == *"="* ]]; then 				   # only use lines containing = should be used
			key=$(echo $line | cut -d '=' -f1) 		   # everything on the left of =
			value=$(echo $line | cut -d '=' -f2-) 	   # everything on the right of =
			overridesArray[$key]=$value				   # store the variable name as key, store the variable name as value
		fi
	done < $overrides_location
	declare overridesArray		# print the resulting array
	echo "---End Part 2---"
	
	echo

	echo "---Start Part 3---"
	echo "Put the overrides into the defaultsArray"
	for override_variable in "${!overridesArray[@]}"; do 
		defaultsArray[$override_variable]=${overridesArray[$override_variable]}
	done
	echo "---End Part 3---"
	
	echo

	echo "---Start Part 4---"
	echo "create an orderedArray"
	echo \#\! \/bin\/bash > $output_location
	while read line; do
		if [[ $line == *"="* ]]; then 				   # only do lines containing = should be used
			key=$(echo $line | cut -d '=' -f1)
			echo $key\=${defaultsArray[$key]} >> $output_location
		fi
	done < $defaults_location

	echo "---End Part 4---"
	
	echo
}

loadGlobals() {
	. ./testautomation_globals_location.incl || aborttest "Could not find testautomation_globals_location to include" #load location of the globals
	. $testautomation_globals_location/testautomation_globals.incl || aborttest "Could not find globals to include" # load the globals
	. $projectfolder_root/$project/vars.incl || aborttest "Could not include project variables"
}

#Function to check if directory exists
isdirectory() {
  if [ -d "$1" ]
  then
    return 0
  else
    return 1
  fi
}

#Function to check if file exists
isfile() {
  if [ -a "$1" ]
  then
    return 0
  else
    return 1
  fi
}

# Function to close silk and remove the error log folders
kill_silk() {
	#Kill al process of silk (2>null redirects the error output to null), 
	#Dit is gedaan omdat je anders een error terug krijgt dat het silk process niet bestond
	echo "Start with killing Silk Performer & Performance Explorer"
	#Controller cleanup
	taskkill /F /IM Performer.exe 2>/dev/null
	taskkill /F /IM PerfExp.exe 2> /dev/null
	sleep 5 #Kill command duurt even
	
	stopSilk=$(ps -W | grep Performer.exe | awk '{print $1}')
	stopPExp=$(ps -W | grep PerfExp.exe | awk '{print $1}')
	
	# echo $stopSilk
	if [[ $stopSilk != "" ]]; then
		echo "Error Silk Performer not stopped"
		exit 1
	fi
	if [[ $stopPExp != "" ]]; then
		echo "Error Performance Explorer not stopped"	
		exit 1
	fi
	
	echo "Done with killing Silk Performer & Performance Explorer"
	
	echo "Start with removing Silk Error logs"
	rm -f -r "C:\Users\Public\Documents\Silk Performer 16.0\Logs"
	echo "Done with removing Silk Error logs"
}

# Function to close jmeter and remove the error log folders
kill_jmeter() {
	#Kill al process of silk (2>null redirects the error output to null), 
	#Dit is gedaan omdat je anders een error terug krijgt dat het silk process niet bestond
	echo "Start with killing Jmeter (Java), could be too destructive"
	if [[ $OS_type == "windows" ]]; then

		#Controller cleanup
		taskkill /F /IM Java.exe 2>/dev/null
		sleep 5 #Kill command duurt even
		
		stopJmeter=$(ps -W | grep -i Java | awk '{print $1}')
		
		# echo $stopJmeter
		if [[ $stopJemeter != "" ]]; then
			echo "Error Jmeter/Java not stopped"
			exit 1
		fi
	elif [[ $OS_type == "linux" ]]; then
		echo "Dit moet nog gemaakt worden!"
	fi
	echo "Done with killing Jmeter/Java"
}

# Function to test if variables are not null or empty
test_variable() {
	if [[ "$1" == null ]] || [[ "$1" == "" ]]; then 
		echo "$1 is empty"
		exit 1
	else
		echo "$1: $2"
	fi
}

# Het runnen van een Silk Performer test
run_silk() {
	echo "_____________________________________________________"
	echo "Start silkperformer performance run with workload: $3"
	
	#echo "scriptfolder: $1"
	#echo "projectname: $2"
	#echo "workload: $3"
	#echo "verification_logdir: $4"
	numberofvars=4
	if [[ "$#" != $numberofvars ]]; then
	  aborttest "Number of arguments does not match, aborting..."
	fi
	
	projectpad=$1
	projectname=$2
	workload=$3
	logdir=$4
	threshold="threshold_$workload"
	
	# verify if threshold is set for this workload, if not: use default setting
	if [[ -z ${!threshold} ]]; then
		threshold="$threshold_default"
		echo "Max test duration not configured for this test type, using default (${!threshold})"
	else
		echo "Max test duration is $threshold (${!threshold})"
	fi
	
	#option 1: $(grep -q "<Workload active=\"true\" name=\"Verificatie\"" "$projectpad/$projectname")
	#option 2: $(grep -q "Workload name=\"$workload\"" "$projectpad/$projectname")
	
	if $(grep -iq "Workload name=\"$workload\"" "$projectpad/$projectname") || $(grep -iq "<Workload active=\"true\" name=\"Verificatie\"" "$projectpad/$projectname"); then
		echo "Workload: $workload found, continuing..."
	else
		aborttest "Workload: $workload not found. Aborting!"
	fi	
	
	# echo
	# echo $scriptfolder
	# echo $projectname
	# echo $workload
	# echo $logdir
	# echo $threshold	
	echo "\"$scriptfolder/$projectname /Automation 10 /Wl:$workload /Resultsdir:$logdir\""
	# echo
	
	
	
	# Running Silk
	if [[ "$workload" == "Verificatie" ]]; then
		cygstart performer "$scriptfolder/$projectname /Automation 10 /Wl:$workload /Resultsdir:$logdir"
		# cygstart performer "$scriptfolder/$projectname /Automation 10 /Wl:$workload"
	else
		cygstart performer "$scriptfolder/$projectname /Automation 10 /Wl:$workload /Resultsdir:$logdir /StartMonitor"
	fi
	
	
	# Start and wait 10 for process to show
	sleep 10
	
	# Get the ProcessID for silkperformer to see if it is still running
	processStart=$(ps -W | grep Performer.exe | awk '{print $1}' | head -1)
	process=$(ps -W | grep Performer.exe | awk '{print $1}' | head -1)
	
	if [[ $process == "" ]]; then
		echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
		echo "Something is wrong with Silk Project, it did not start"
		echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
		exit 1
	fi
	
	# Get some timestamps in order to be able to tell hoe long the test is running
	tstart=`date +"%s"`
	tcurrent=`date +"%s"`
	tdiff=`expr $tcurrent - $tstart`
	
	# If the Silk Performer process is still in the process list we expect that the test is still running
	while [[ "$processStart" == "$process" ]]
	do
		echo "Performance test in progress, running for: $tdiff seconds"
		
		# Prep for next while loop and make sure Silkperformer does not run for too long
		if [[ $threshold -gt $tdiff ]]; then
			process=$(ps -W | grep Performer.exe | awk '{print $1}' | head -1)
		else
			echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
			echo "Treshold exceeded aborting test..."
			echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
			exit 1
		fi
		
		if [[ "$workload" == "Verificatie" ]]; then
			sleep 10
		else
			sleep 60
		fi
		
		tcurrent=`date +"%s"`
		tdiff=`expr $tcurrent - $tstart`
		##	
	done
	
	sleep 10
	
	echo "Done with performance run with workload: $3"
	echo "_____________________________________________________"
	
}

# Create a new testplan copy with the right workload enabled
preparejmeterscript() {
	projectname=$1
	workload=$2
	scriptfolder=$3
	
	sourcefilename=$scriptfolder/$projectname.jmx
	targetfilename=$scriptfolder/"$projectname"_"$workload".jmx

	echo "Prepare workload script from base script, project [$projectname] workload [$workload]..."

	# check if workload config is present	
	if grep -q "WORKLOAD_$workload" "$sourcefilename"
	then
		echo "Workload [$workload] found in file [$sourcefilename]"
	else
		echo "Fatal: workload [$workload] not found in source file [$sourcefilename], exiting..."
		exit 1
	fi

	cp $sourcefilename $targetfilename

	# disable all workload configurations
	sed -i "/testname=.WORKLOAD_/s/true/false/" "$targetfilename"

	# enable configured workload
	sed -i "/testname=.WORKLOAD_$workload.\s/s/false/true/" "$targetfilename"

	# check if at least one workload is enabled
	if grep -q 'WORKLOAD_.*true' $targetfilename
	then
		echo "Workload successfully enabled in file [$targetfilename]"
	else
		echo "Fatal: could not create script [$targetfilename] from [$sourcefilename]"
		
		echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
		echo "Unable to prepare jmeter script"
		echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
			
		exit 1
	fi

	echo "Done preparing testscript"
}

# Het runnen van een jmeter test
run_jmeter() {
	echo "_____________________________________________________"
	echo "Start jmeter performance run with workload: $3"
	
	numberofvars=4
	if [[ "$#" != $numberofvars ]]; then
	  aborttest "Number of arguments does not match, aborting..."
	fi
	
	scriptfolder=$1
	projectname=$2
	workload=$3
	logdir=$4
	threshold="threshold_$workload"
	
	test_variable "OS_type variable" $OS_type
	
	# verify if threshold is set for this workload, if not: use default setting
	if [[ -z ${!threshold} ]]; then
		threshold="threshold_default"
		echo "Max test duration not configured for this test type, using default (${!threshold})"
	else
		echo "Max test duration is $threshold (${!threshold})"
	fi
	
	echo "Projectname: $projectname"
	
	# prepare new script from source with right workload enabled
	preparejmeterscript $projectname $workload $scriptfolder
	
	if isfile $scriptfolder/${projectname}_$workload.jmx; then echo "Script found for $workload workload, continuing..."; else aborttest "Script not found for $workload workload. Aborting!"; fi  
	
	cd ${loadgendir_jmeter}
	#cd /cygdrive/e/"06_Tools/jmeter/bin"
	

	# Run jmeter
	if [[ $OS_type == "windows" || $OS_type == "linux" ]]; then
		./jmeter/bin/jmeter.sh -t "$scriptfolder/${projectname}_$workload.jmx" -Jresultfile=$logdir/result.jtl -n -Dsummariser.name=summary -Dsummariser.interval=60 -Dsummariser.log=true -Dsummariser.out=true -Djavax.net.ssl.keyStore=$jm_keystore -Djavax.net.ssl.keyStorePassword=$jm_keyStorePassword &
#		./jmeter/bin/jmeter.sh -t "$scriptfolder/${projectname}_$workload.jmx" -Jresultfile=$logdir/result.jtl -n -Dsummariser.name=summary -Dsummariser.interval=60 -Dsummariser.log=true -Dsummariser.out=true &
	else
		aborttest "\$OS_type in globals is not windows or linux but is: \"$OS_type\", aborting test..."
	fi
	
	# Start and wait 10 for process to show
	sleep 10
	
	# Get the ProcessID for silkperformer to see if it is still running
	if [[ $OS_type == "windows" ]]; then
		processStart=$(ps -a | grep -i Java | awk '{print $1}' | head -1)
		process=$(ps -a | grep -i Java | awk '{print $1}' | head -1)
	elif [[ $OS_type == "linux" ]]; then
		processStart=$(ps -a | grep jmeter.sh | awk '{print $1}' | head -1)
		process=$(ps -a | grep jmeter.sh | awk '{print $1}' | head -1)
	fi
	
	if [[ $process == "" ]]; then
		echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
		echo "Something is wrong with Jmeter, it did not start"
		echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
		exit 1
	fi
	
	# Get some timestamps in order to be able to tell hoe long the test is running
	tstart=`date +"%s"`
	tcurrent=`date +"%s"`
	tdiff=`expr $tcurrent - $tstart`
	
	# If the Jmeter process is still in the process list we expect that the test is still running
	while [[ "$processStart" == "$process" ]]
	do
		# echo "Performance test in progress, running for: $tdiff seconds"
		
		# Prep for next while loop and make sure Jmeter does not run for too long
		if [[ $threshold -gt $tdiff ]]; then
			if [[ $OS_type == "windows" ]]; then
				process=$(ps -a | grep -i Java | awk '{print $1}' | head -1)
			elif [[ $OS_type == "linux" ]]; then
				process=$(ps -a | grep jmeter.sh | awk '{print $1}' | head -1)
			fi
		else
			echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
			echo "Treshold exceeded aborting test..."
			
			if [[ $OS_type == "windows" ]]; then
				taskkill /F /IM Java.exe 2>/dev/null
			elif [[ $OS_type == "linux" ]]; then
				kill -9 jmeter.sh
			fi
			
			# Making a copy of the JMeter log for reference
			cp ${loadgendir_jmeter}/jmeter.log $logdir_root/jmeter_${workload}.log
			cp ${loadgendir_jmeter}/jmeter.log $logbackupdir/$testtag/jmeter_${workload}.log

			# Making a copy of the testresults for reference
			cp -f -r $loadtest_logdir/. $logbackupdir/$testtag/testresults/
			
			echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
			exit 1
		fi
		
		if [[ "$workload" == "Verificatie" ]]; then
			sleep 10
		else
			sleep 60
		fi
		
		tcurrent=`date +"%s"`
		tdiff=`expr $tcurrent - $tstart`
		##	
	done
	
	sleep 10
	
	# Making a copy of the JMeter log for reference
	cp ${loadgendir_jmeter}/jmeter.log $logdir_root/jmeter_${workload}.log
	cp ${loadgendir_jmeter}/jmeter.log $logbackupdir/$testtag/jmeter_${workload}.log
	
	echo "Done with performance run with workload: $3"
	echo "_____________________________________________________"
	
}

# check of silk resultaten gegenereerd zijn
succescheck_silk () {
	if isfile $loadtest_logdir/detailedReport.xml; then echo "Found the results file, continuing..."; else aborttest "Results not found! Test run failed? Aborting run"; fi 
}

# check of JMeter resultaten gegenereerd zijn
succescheck_jmeter () {
	echo "$loadtest_logdir/result.jtl"
	if isfile $loadtest_logdir/result.jtl; then echo "Found the results file, continuing..."; else aborttest "Results not found! Test run failed? Aborting run"; fi 
}

# Extract faults Silk
extractfault_silk () {

	if isfile $verification_logdir/baselineReport.brp; then echo "Found the file containing error information, checking results now"; else aborttest "File containig errors not found, cannot display errors. Aborting test"; fi 
	
	# Print output van baselineReport
	# Filter zodat alleen stuk tussen MessageList overblijft (hier staan de fouten in het rapport)
	# Filter hier de transactions uit
	# Pak de text tussen <Transaction> en </Transaction>
	# Dedupliceer
	foutetransactions=$(cat $verification_logdir/baselineReport.brp | sed -n "/<MessageList>/,/<\/MessageList>/p" | grep -e "<Transaction>" | sed -e 's/<Transaction>\(.*\)<\/Transaction>/\1/' | uniq)
	echo "-------------------------"
	echo "The following transaction(s) are reporting an error"
	echo $foutetransactions
	echo "-------------------------"
}

# Extract faults JMeter
extractfault_jmeter () {
	
	if isfile $verification_logdir/result.jtl; then echo "Found the file containing error information, checking results now"; else aborttest "File containig errors not found, cannot display errors. Aborting test"; fi 
	
	# Print output van results.jtl
	# Filter op gefaalde transacties
	# Pak de text tussen lb=" en rc
	# Dedupliceer
	echo "-------------------------"
	echo "The following transaction(s) are reporting an error"
	cat $verification_logdir/result.jtl | grep  "s=\"false\"" | sed -e 's/.*lb=\"\(.*\)" rc.*/\1/' | uniq
	echo "-------------------------"
}

# check of Silk validatie resultaten gegenereerd zijn
validation_check_silk () {
	## Check if validation run ran without failed transactions
	if isfile $verification_logdir/detailedReport.xml; then echo "Found the results file, checking results now"; else aborttest "Results not found Verification run failed? Aborting test"; fi 
	
	#----test----
	movelogs_src=$verification_logdir
	movelogs_dst=$logbackupdir/$testtag/verification_testresults
	
	echo source: $movelogs_src
	echo destination: $movelogs_dst
	
	mkdir -p $movelogs_dst
	cp -f -r $movelogs_src/. $movelogs_dst/
	#------------
	
	
	failedorsucces=$(grep -E -o '<TransactionsFailed>.*</TransactionsFailed>' $verification_logdir/detailedReport.xml | grep -E -o '[0-9]+' | awk '{ SUM += $1} END { print SUM }')
	echo "Errors found: "$failedorsucces
	
	## Check if the loadtestResult was generated
	if [[ "" == "$failedorsucces" ]]; then
		if [[ "$abortifverifyfailed" == "true" ]]; then
			echo "Error: No detailedReport.xml found! Aborting..."
			exit 1
		else
			extractfault_silk
			echo "Abort Verification when result is not found is disabled"
		fi
	fi	
	
	if [[ "0" != "$failedorsucces" ]]; then
		if [[ "$abortifverifyfailed" == "true" ]]; then
			echo "Error: Failed Transactions found. Checking for faulty transactions"
			extractfault_silk
			exit 1
		else
			extractfault_silk
			echo "Abort verification when failed disabled"
		fi
	fi
}

# check of JMeter validatie resultaten gegenereerd zijn
validation_check_jmeter () {
	## Check if validation run ran without failed transactions
	#echo "$verification_logdir/result.jtl"
	if isfile $verification_logdir/result.jtl; then echo "Found the results file, checking results now"; else aborttest "Results not found Verification run failed? Aborting test"; fi 
	
	#----test----
	movelogs_src=$verification_logdir
	movelogs_dst=$logbackupdir/$testtag/verification_testresults
	
	echo source: $movelogs_src
	echo destination: $movelogs_dst
	
	mkdir -p $movelogs_dst
	cp -f -r $movelogs_src/. $movelogs_dst/
	#------------
	
	failedorsucces=$(cat $verification_logdir/result.jtl | grep s=\"false\" | wc -l)
	echo "Errors found: "$failedorsucces
	
	if [[ "0" != "$failedorsucces" ]]; then
		if [[ "$abortifverifyfailed" == "true" ]]; then
			echo "Error: Failed Transactions found, aborting..."
			extractfault_jmeter
			exit 1
		else
			extractfault_jmeter
			echo "Abort verification when failed disabled"
		fi
	fi
}

# Het updaten van de repository om er zeker van te zijn dat de juiste versie van het script gebruikt word
update_repository () {
	
		if [[ "$1" == "SVN" ]]; then		
			echo "Starting SVN Update"
			
			echo "Als dit weer gebruikt gaat worden eerst kijken naar variabelen"
			
			cd $svndrive
			cd $svnfolder
			svn cleanup
			svn update $testversion
			svn cleanup

			if isdirectory $scriptfolder; then echo "$scriptfolder correctly pulled from SVN"; else aborttest "Failure pulling \"$scriptfolder\" from SVN!, aborting..."; fi 
			echo "Done SVN Update"
		elif [[ "$1"  == "GIT" ]]; then
			echo "Start with GIT Pull"
			cd $gitdrive
			cd $gitfolder
			git checkout $testversion		
			echo "Done with GIT Pull"			
		else
			echo "Unknown repository type. Only SVN & GIT are allowed."
		fi
		
}

#commiten van rapport naar GIT
commit_to_GIT () {
	## Resultaten in GIT zetten
	echo " -- Commit results to GIT"
	
	cd $repository_log_drive
	cd $repository_report_dir/$workload
	
	echo " -- Start adding files to git"
	git add .
	echo " -- Done adding files to git"

	echo " -- Start git commit"
	git commit -am "Automatische commit vanuit de teststraat"
	echo " -- Done git commit"

	echo " -- Start git push"
	git push $giturl --all
	echo " -- Done git push"

	echo " -- Done commiting results to GIT"
}

setAppVersion() {
	appVersion=$1
}

getAppVersion() {
	return $appVersion
}

validateDiskSpace(){
	location=$1
	reqSpace=$2
	reqSpaceWarning=$3
	
	if [[ "$OS_type" == "windows" ]]; then
		availSpace=$(df "$location"":/" --block-size=MB | awk 'NR==2 { print $4 }' | sed 's/[^0-9]*//g')
	elif [[ "$OS_type" == "linux" ]]; then
		availSpace=$(df "$location" --block-size=MB | awk 'NR==2 { print $4 }' | sed 's/[^0-9]*//g')
	else
		echo "Unknown OS Type, please define variable OS_type in the globals to either 'windows' or 'linux'"
		exit 1
	fi
	
	if [[ $availSpace -lt $reqSpace ]]; then
		echo "*****************************"
		echo "Not enough diskspace available on $1" 
		echo "Required space (in MB)  = $reqSpace"
		echo "Available space (in MB) = $availSpace"
		echo "Aborting test"
		echo "*****************************"
		exit 1
	elif [[ $availSpace -lt $reqSpaceWarning ]]; then
		echo "*****************************"
		echo "Warning disk space is getting limited on $1" 
		echo "Required space (in MB)  = $reqSpaceWarning"
		echo "Available space (in MB) = $availSpace"
		echo "*****************************"
	else
		echo "Enough space available on $location: $availSpace MB, required: $reqSpace"
	fi
}

cleanbackupfolder(){
	# Cleaning backup files
	echo "Start removing backup result and logging folders"
	
	echo "Backup directory: $logbackupdir"
	#echo "Project: $project"

	arrResultDirs=(`find "$logbackupdir" -maxdepth 1 -type d -regex ".*\/[0-9][0-9]-[^ ]*_$project" | sort -r`)
	echo "Keep numer of backups for this environment: $backupsToKeep"
	echo "Current number of backups: ${#arrResultDirs[@]}"

	echo "The following backup folders are deleted: "
	# Aangegeven aantal resultaten laten staan
	for i in "${arrResultDirs[@]:$backupsToKeep:${#arrResultDirs[@]}-$backupsToKeep}"; do echo "$i"; done
	
	echo "Results: "
	# Daadwerkelijk verwijderen files en directories, tevens weergeven (-v verbose)
	for i in "${arrResultDirs[@]:$backupsToKeep:${#arrResultDirs[@]}-$backupsToKeep}"; do rm -r -v "$i"; done

	unset arrResultDirs;
	arrResultDirs=(`find "$logbackupdir" -maxdepth 1 -type d -regex ".*\/[0-9][0-9]-[^ ]*_$project" | sort -r`)
	echo "Remaining number of backups: ${#arrResultDirs[@]}"
	unset arrResultDirs;

	echo "Done removing backup result and logging folders"
}
