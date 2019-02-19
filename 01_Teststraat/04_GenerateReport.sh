#!/bin/bash
echo "#######################################################################"
echo "Start Script '04_GenerateReport.sh' @ `date +"%T"`"
echo "-----------------------------------------------------------------------"
echo "Start with setting and checking incomming variables"

testtag=$1
Pworkload=$2
baseline=$3

# even aanzetten om te testen
# project="ivs-next"

. functions.sh || aborttest "Could not include functions"
loadGlobals
. $projectfolder_root/$project/vars.incl || aborttest "Could not include project variables"

test_variable "testtag" $testtag
test_variable "Pworkload" $Pworkload
test_variable "baseline" $baseline

echo "Done with setting and checking incomming variables"
echo "-----------------------------------------------------------------------"
echo

echo
echo "-----------------------------------------------------------------------"
# Genereren van het rapport
if [[ "$generatereport" == "true" ]]; then
	
	echo "Begin with generating report"

	# Genereren report template
	echo "Report template: $reporttemplatefolder"
	echo "project: $project"
	cd "$reporttemplatefolder"
	pwd
	. ./generateTemplate.sh $project $projectfolder_root/$project/Transactions.csv || aborttest "Something went wrong generating the template"

	# Kopieeren report template naar folder
	mkdir -p $resulttemplatefolder
	cp -f -v "${reporttemplatefolder}\\templates\\${project}_report_template.html" "${resulttemplatefolder}"
	
	# Genereren report
	mkdir -p $logdir_root\\report
	echo "report tool folder: $reporttoolfolder"
	echo "project: $project"
	cd "$reporttoolfolder"
	pwd
	cygstart  -w ./report_wrapper.bat $project $tool "\"Gegenereerd vanuit de TestStraat\"" $baseline
	
	# Check of rapport gegenereerd is en of er een errorcode is
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

echo
echo "-----------------------------------------------------------------------"

# Copying the generated report to backup structure
if [[ "$move_report" == "true" ]]; then

	echo "Start copy report"

    movedir_src=$logdir_root\\report
	movedir_dst=$logbackupdir\\$testtag\\report
	movedir_pub=$logbackupdir\\publish\\report
	
	echo source: $movedir_src
	echo destination: $movedir_dst

	echo "Copy report for backup..."
	
	mkdir -p $movedir_dst
	if isdirectory $movedir_dst; then echo "Backup report dir created"; else aborttest "Could not create $movedir_dst - abort test"; fi 
	cp -f -r $movedir_src\\. $movedir_dst\\

	
		
	#echo create index from ${project}_report.html
	#cp -f -r $movedir_src\\${project}_report.html $movedir_dst\\index.html

	echo "Done copy report"
fi

echo "-----------------------------------------------------------------------"
echo

echo
echo "-----------------------------------------------------------------------"
# Generate a report history
if [[ "$generatereporthistory" == "true" ]]; then
	
	echo "Copy report for publishing..."
	
	mkdir -p $reportpublishfolder
	movedir_src=$logdir_root\\report
	
	# echo source: $movedir_src
	# echo destination: $reportpublishfolder
	
	# copy to pushish for publishing purpose
	if isdirectory $reportpublishfolder; then echo "Publish report dir created"; else aborttest "Could not create $reportpublishfolder - abort test"; fi 
	cp -f -r $movedir_src\\. $reportpublishfolder\\	
	
else
	echo "Commiting to Repository is disabled"
fi
echo "-----------------------------------------------------------------------"
echo


echo
echo "-----------------------------------------------------------------------"
# Committing of report to repository
if [[ "$committorepository" == "true" ]]; then
	
	echo "Start with commiting to Repository"
	
	cd $repository_log_drive
	cd $repository_report_dir\\$Pworkload
	
	echo "repository log drive: $repository_log_drive"
	echo "repository log drive: $repository_report_dir"
	
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
	cp -f -r "$logdir_root\\report\\${project}_report.html" "$repository_report_dir\\$Pworkload\\index.html"
	cp -f -r "$logdir_root\\report\\js" "$repository_report_dir\\$Pworkload"
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