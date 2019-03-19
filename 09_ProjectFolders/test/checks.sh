echo "-----------------------------------------------------------------------"
## Valideer of er genoeg variabelen meegegeven worden
if [[ "$checkforarguments" == "true" ]]; then
	
	echo "The script received $# arguments"
	echo "The script expected "$numberofvars" arguments"

	if [[ "$#" != $numberofvars ]]; then
	  echo "Number of arguments does not match, aborting..."
	  exit
	fi
	echo "The number of arguments supplied is correct continuing..."
	
	echo "Start printing incomming arguments"
	argnum=0
	for ARG in $*
	do
		argnum=$(expr $argnum + 1)
		echo "Argument $argnum: $ARG"
	done
	echo "Done printing incomming arguments"
	
else
	echo "Checking for arguments is disabled"
fi
echo "-----------------------------------------------------------------------"

echo "-----------------------------------------------------------------------"
if [[ "$checkdiskspace" == "true" ]]; then
	
	echo "Checking Disk Space on C:"

	availablespaceC=$(df -B m C:/ | awk '{ print $4 }' | grep -E -o '[0-9]+')

	if [[ $freespaceCthreshold -lt $availablespaceC  ]]; then
		echo "Enough space free on C:\\ ($availablespaceC) MB"
	else
		echo "Warning 'C:\\' has only $availablespaceC MB free"
	fi
	
	# E
	echo "Checking Disk Space on E:"

	availablespaceE=$(df -B m E:/ | awk '{ print $4 }' | grep -E -o '[0-9]+')

	if [[ $freespaceEthreshold -lt $availablespaceE  ]]; then
		echo "Enough space free on E:\\ ($availablespaceE) MB"
	else
		echo "Warning 'E:\\' has only $availablespaceE MB free"
	fi
	
else
	echo "Check disk space is disabled"
fi
echo "-----------------------------------------------------------------------"
