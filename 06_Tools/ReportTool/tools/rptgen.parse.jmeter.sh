#!/bin/bash

echo "Parse [JMeter] START"

echo loadgendir=$rpt_loadgendir
echo temppath=$rpt_temppath
echo toolspath=$rpt_toolspath

#error handling
aborttest_parse() {
	echo "STOP, error in rptgen.parse.jmeter ($1)"
	rpt_exitcode=1
	#exit 0
}

echo Convert Jmeter log to csv...

orgpath=$('pwd')
cd $rpt_loadgendir

echo convert jtl to transaction csv: all
. JMeterPluginsCMD.sh --generate-csv $rpt_temppath/_transactions_all.csv --input-jtl $rpt_temppath/_transactions.jtl --plugin-type AggregateReport
if [ $? -ne 0 ]; then aborttest_parse "csv convert all trs"; return; fi

echo convert jtl to transaction csv: success only
. JMeterPluginsCMD.sh --generate-csv $rpt_temppath/_transactions_success.csv --input-jtl $rpt_temppath/_transactions.jtl --plugin-type AggregateReport --success-filter true
if [ $? -ne 0 ]; then aborttest_parse "csv convert success trs"; return; fi

cd $orgpath

echo Parse transactiondata to intermediate...
dotnet $rpt_toolspath/rpg.parsetransactions.dll parser=jmeter transactionfilecsv_success=$rpt_temppath/_transactions_success.csv transactionfilecsv_all=$rpt_temppath/_transactions_all.csv intermediatefile=$rpt_temppath/_intermediate.trs.csv intermediatefilevars=$rpt_temppath/_intermediate.var.trs.csv
if [ $? -ne 0 ]; then aborttest_parse "parse transactions"; return; fi

echo Parse measures to intermediate...
dotnet $rpt_toolspath/rpg.parsemeasures.dll parser=jmeter transactionfilejtl=$rpt_temppath/_transactions.jtl intermediatefile=$rpt_temppath/_intermediate.msr.csv intermediatefilevars=$rpt_temppath/_intermediate.var.msr.csv
if [ $? -ne 0 ]; then aborttest_parse "parse measures"; return; fi

echo Parse variables to intermediate...
dotnet $rpt_toolspath/rpg.parsevariables.dll parser=jmeter transactionfilejtl=$rpt_temppath/_transactions.jtl transactionfilecsv=$rpt_temppath/_transactions_all.csv intermediatefile=$rpt_temppath/_intermediate.var.csv
if [ $? -ne 0 ]; then aborttest_parse "parse variables"; return; fi

echo "Parse [JMeter] DONE"
