#!/bin/bash

echo "Parse [Silkperformer] START"

echo loadgendir=$rpt_loadgendir
echo temppath=$rpt_temppath
echo toolspath=$rpt_toolspath

#error handling
aborttest() {
	echo "STOP, error in rptgen.parse.silk ($1)"
	exit 1
}

echo "Convert Silkperformer transaction log (tsd) to csv..."
cd "$rpt_loadgendir" 
cygstart Tsd2Csv.exe "$rpt_temppath\\_transactions.tsd $rpt_temppath\\_transactions.csv -Delimiter ; -DecimalPoint ,"
if [ $? -ne 0 ]; then aborttest "silk convert tsd to csv"; fi

sleep 5

echo Parse transactiondata to intermediate...
$rpt_toolspath/srt.parsetransactions.exe parser=silkperformer transactionfilebrp=$rpt_temppath/_transactions.brp transactionfilecsv=$rpt_temppath/_transactions.csv intermediatefile=$rpt_temppath/_intermediate.trs.csv
if [ $? -ne 0 ]; then aborttest "parse transactions"; fi

echo Parse measures to intermediate...
$rpt_toolspath/srt.parsemeasures.exe parser=silkperformer transactionfilecsv=$rpt_temppath/_transactions.csv intermediatefile=$rpt_temppath/_intermediate.msr.csv
if [ $? -ne 0 ]; then aborttest "parse measures"; fi

echo Parse variables to intermediate...
$rpt_toolspath/srt.parsevariables.exe parser=silkperformer transactionfilecsv=$rpt_temppath/_transactions.csv transactionfilebrp=$rpt_temppath/_transactions.brp intermediatefile=$rpt_temppath/_intermediate.var.csv
if [ $? -ne 0 ]; then aborttest "parse variables"; fi

echo "Parse [Silkperformer] END"
