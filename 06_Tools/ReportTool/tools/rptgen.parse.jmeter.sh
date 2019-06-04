#!/bin/bash

echo "Parse [JMeter] START"

echo loadgendir=$rpt_loadgendir
echo temppath=$rpt_temppath
echo toolspath=$rpt_toolspath

#error handling
aborttest() {
	echo "STOP, error in rptgen.parrseandimport.jmeter ($1)"
	exit 1
}

echo Convert Jmeter log to csv...

orgpath=$('pwd')
cd $rpt_loadgendir

echo convert jtl to transaction csv: all
. JMeterPluginsCMD.sh --generate-csv $rpt_temppath/_transactions_all.csv --input-jtl $rpt_temppath/_transactions.jtl --plugin-type AggregateReport
if [ $? -ne 0 ]; then aborttest "csv convert all trs"; fi

echo convert jtl to transaction csv: success only
. JMeterPluginsCMD.sh --generate-csv $rpt_temppath/_transactions_success.csv --input-jtl $rpt_temppath/_transactions.jtl --plugin-type AggregateReport --success-filter true
if [ $? -ne 0 ]; then aborttest "csv convert success trs"; fi

cd $orgpath

echo Parse transactiondata to intermediate...
dotnet $rpt_toolspath/rpg.parsetransactions.dll parser=jmeter transactionfilecsv_success=$rpt_temppath/_transactions_success.csv transactionfilecsv_all=$rpt_temppath/_transactions_all.csv intermediatefile=$rpt_temppath/_intermediate.trs.csv intermediatefilevars=$rpt_temppath/_intermediate.var.trs.csv
if [ $? -ne 0 ]; then aborttest "parse transactions"; fi

echo Parse measures to intermediate...
dotnet $rpt_toolspath/rpg.parsemeasures.dll parser=jmeter transactionfilejtl=$rpt_temppath/_transactions.jtl intermediatefile=$rpt_temppath/_intermediate.msr.csv intermediatefilevars=$rpt_temppath/_intermediate.var.msr.csv
if [ $? -ne 0 ]; then aborttest "parse measures"; fi

echo Parse variables to intermediate...
dotnet $rpt_toolspath/rpg.parsevariables.dll parser=jmeter transactionfilejtl=$rpt_temppath/_transactions.jtl transactionfilecsv=$rpt_temppath/_transactions_all.csv intermediatefile=$rpt_temppath/_intermediate.var.csv
if [ $? -ne 0 ]; then aborttest "parse variables"; fi

echo "Parse [JMeter] DONE"
