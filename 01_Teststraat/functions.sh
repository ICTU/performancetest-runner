#!/bin/bash

# function to end script
aborttest() {
	echo $1
	exit 1
}

loadGlobals() {
	#echo "Start loading globals"
	# Check if there is a testautomation_globals.incl file in the test automation root folder
	if isfile ./testautomation_globals_location.incl; then 
		# echo "Found location of test automation global variables"
		. ./testautomation_globals_location.incl
	else 
		echo "Could not find ./testautomation_globals_location creating it now"
		echo "testautomation_globals_location=\"E:/00_Globals/testautomation_globals.incl\"" > testautomation_globals_location.incl
		aborttest
	fi
		
	if isfile $testautomation_globals_location; then 
		echo "testautomation_globals found, including now"; 
		. $testautomation_globals_location
	else
		echo "testautomation_globals_location: $testautomation_globals_location"
		echo "Could not find location of global test automation variables! Copying template to testautomation_globals_location"
		echo "current location: $(pwd)"
		cp ./template/testautomation_globals.incl $testautomation_globals_location
		aborttest "Aborting test due to testautomation_globals not present. Template is created now, please fill it with the correct locations before running test again!"
	fi 
	#echo "Done loading globals"
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
	if [[ $OS == "windows" ]]; then

		#Controller cleanup
		taskkill /F /IM Java.exe 2>/dev/null
		sleep 5 #Kill command duurt even
		
		stopJmeter=$(ps -W | grep -i Java | awk '{print $1}')
		
		# echo $stopJmeter
		if [[ $stopJemeter != "" ]]; then
			echo "Error Jmeter/Java not stopped"
			exit 1
		fi
	elif [[ $OS == "linux" ]]; then
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

	cp $sourcefilename $targetfilename

	# disable all workload configurations
	sed -i "/testname=.WORKLOAD_/s/true/false/" "$targetfilename"

	# enable configured workload
	sed -i "/testname=.WORKLOAD_$workload.\s/s/false/true/" "$targetfilename"

	# check if at least one workload is enabled
	if grep -q 'WORKLOAD_.*true' $targetfilename
	then
		echo "Testplan generated to file [$targetfilename]"
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
	./jmeter/bin/jmeter.sh -t "$scriptfolder/${projectname}_$workload.jmx" -Jresultfile=$logdir/result.jtl -n -Dsummariser.name=summary -Dsummariser.interval=60 -Dsummariser.log=true -Dsummariser.out=true &
	
	# Start and wait 10 for process to show
	sleep 10
	
	# Get the ProcessID for silkperformer to see if it is still running
	if [[ $OS == "windows" ]]; then
		processStart=$(ps -a | grep -i Java | awk '{print $1}' | head -1)
		process=$(ps -a | grep -i Java | awk '{print $1}' | head -1)
	elif [[ $OS == "linux" ]]; then
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
			if [[ $OS == "windows" ]]; then
				process=$(ps -a | grep -i Java | awk '{print $1}' | head -1)
			elif [[ $OS == "linux" ]]; then
				process=$(ps -a | grep jmeter.sh | awk '{print $1}' | head -1)
			fi
		else
			echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
			echo "Treshold exceeded aborting test..."
			
			if [[ $OS == "windows" ]]; then
				taskkill /F /IM Java.exe 2>/dev/null
			elif [[ $OS == "linux" ]]; then
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
	availSpace=$(df "$location" --block-size=G | awk 'NR==2 { print $4 }' | sed 's/[^0-9]*//g')
	if (( availSpace < freespace_$location\_threshold )); then
	  echo "*****************************"
	  echo "Not enough diskspace available" 
	  echo "Required space  = $reqSpace"
	  echo "Available space = $availSpace"
	  echo "Aborting test"
	  echo "*****************************"
	  exit 1
	else
		echo "Genoeg vrij: $availSpace"
	fi
}