#!/bin/bash
echo "#######################################################################"
echo "Start Script '04_GenerateReport.sh' @ `date +"%T"`"
echo "-----------------------------------------------------------------------"
echo "Start with setting and checking incomming variables"

testtag=$1
workload=$2
baseline=$3

# Can be enabled to do test
# project="<projectname>"

. functions.sh || aborttest "Could not include functions"
loadGlobals

test_variable "testtag" $testtag
test_variable "workload" $workload
test_variable "baseline" $baseline

echo "Done with setting and checking incomming variables"
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Generation of the report
if [[ "$generatereport" == "true" ]]; then
	
	echo "Begin with generating report"

	# First generate the template
	echo "Report template: $reporttemplategenerator_root"
	echo "project: $project"
	cd "$reporttemplategenerator_root"
	pwd
	. ./generateTemplate.sh $project $projectfolder_root/$project/Transactions.csv || aborttest "Something went wrong generating the template"

	# Copy the template to the report tool folder
	mkdir -p $reporttemplatedestinationfolder
	cp -f -v "${reporttemplategenerator_root}/templates/${project}_report_template.html" "${reporttemplatedestinationfolder}"
	
	# Genereren report
	mkdir -p $loadtest_report
	echo "report tool folder: $reporttoolfolder"
	echo "project: $project"
	cd "$reporttoolfolder"
	pwd
	#cygstart  -w ./report_wrapper.bat $project $tool "\"Gegenereerd vanuit de TestStraat\"" $baseline
	. ./report_wrapper.sh $project $tool "\"Gegenereerd vanuit de TestStraat\"" $baseline
	
	# Check if the report is generated or if there is an error code
	if isfile $logdir_root/report/${project}_report.exitcode; then echo "Checking for errors..."; else aborttest "Warning, no error file found and therefore also no report generated!"; fi 
	reporterrorcode=$(grep -o '[0-9]*' $logdir_root/report/${project}_report.exitcode)
	if [[ "0" == "$reporterrorcode" ]]; then
		echo "No error found when creating report"
		if isfile $logdir_root/report/${project}_report.html; then echo "A report is generated"; else aborttest "No report found, aborting test"; fi 
	elif [[ "1" == "$reporterrorcode" ]]; then
		echo "Unknown error occurred"
		aborttest "Abort. Report not properly generated."
	elif [[ "2" == "$reporterrorcode" ]]; then
		echo "Exeption raised while parsing the results"
		aborttest "Abort. Report not properly generated."
	elif [[ "10" == "$reporterrorcode" ]]; then
		echo "No useful transaction data found"
		aborttest "Abort. Report not properly generated."
	else
		echo "Error code found: $reporterrorcode"
		aborttest "Abort. Report not properly generated."
	fi
	echo "Done with generating report" 
	
else
	echo "Generate report disabled"
fi
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"

# Copying the generated report to backup structure
if [[ "$move_report" == "true" ]]; then

	echo "Start copy report"

    movedir_src=$loadtest_report
	movedir_dst=$logbackupdir/$testtag/report
	movedir_pub=$logbackupdir/publish/report
	
	echo source: $movedir_src
	echo destination: $movedir_dst

	echo "Copy report for backup..."
	
	mkdir -p $movedir_dst
	if isdirectory $movedir_dst; then echo "Backup report dir created"; else aborttest "Could not create $movedir_dst - abort test"; fi 
	cp -f -r $movedir_src/. $movedir_dst/

	
		
	#echo create index from ${project}_report.html
	#cp -f -r $movedir_src/${project}_report.html $movedir_dst/index.html

	echo "Done copy report"
fi

echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Generate a report history
if [[ "$generatereporthistory" == "true" ]]; then
	
	echo "Copy report for publishing..."
	
	mkdir -p $reportpublishfolder
	movedir_src=$loadtest_report
	
	# echo source: $movedir_src
	# echo destination: $reportpublishfolder
	
	# copy to pushish for publishing purpose
	if isdirectory $reportpublishfolder; then echo "Publish report dir created"; else aborttest "Could not create $reportpublishfolder - abort test"; fi 
	cp -f -r $movedir_src/. $reportpublishfolder/	
	
else
	echo "Generate Report History is disabled"
fi
echo "-----------------------------------------------------------------------"

echo

echo "-----------------------------------------------------------------------"
# Committing of report to repository
if [[ "$committorepository" == "true" ]]; then
	
	echo "Start with commiting to Repository"
	
	cd $repository_log_drive
	cd $repository_report_dir/$workload
	
	echo "repository log drive: $repository_log_drive"
	echo "repository log dir: $repository_report_dir"
	
	echo "Pull GIT just in case"
	echo "GitCustomURL: $giturl"
	
	# ingebouwd omdat bij initiele run er nog niks in de repo staat en daarom de repo nog leeg is en niet gepulled kan worden
	aantalBestanden=$(ls | wc -l)
	if [[ $aantalBestanden -eq 0 ]]; then
		echo "Lege repo, geen pull nodig"
	else
		git pull $giturl || aborttest "GIT Pull failed, aborting"
	fi
	
	echo "Pull Git done"
	
	# Start copying report to repository directory
	echo "Start with copying report to the repository directory"
	cp -f -r "$loadtest_report/${project}_report.html" "$repository_report_dir/$workload/index.html"
	#cp -f -r "$loadtest_report/js" "$repository_report_dir/$workload"
	echo "Done with copying report to the repository Directory"
	
	# The true commit to the repository
	echo " - Start with commiting to repository"
	commit_to_GIT
	echo " - Done with commiting to repository"
	
	# Back to default location
	cd $testautomation_root
	
else
	echo "Commiting to Repository is disabled"
fi
echo "-----------------------------------------------------------------------"
echo "End Script '04_GenerateReport.sh' @ `date +"%T"`"
echo "#######################################################################"