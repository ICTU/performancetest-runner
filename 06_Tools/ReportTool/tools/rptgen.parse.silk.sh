#!/bin/bash

echo "Parse [Silkperformer] START"

echo loadgendir=$rpt_loadgendir
echo temppath=$rpt_temppath
echo toolspath=$rpt_toolspath

#error handling
aborttest_parse() {
	echo "STOP, error in rptgen.parse.silk ($1)"
	rpt_exitcode=1
	#exit 0
}

echo "Convert Silkperformer transaction log (tsd) to csv..."
cd "$rpt_loadgendir" 
cygstart Tsd2Csv.exe "$rpt_temppath\\_transactions.tsd $rpt_temppath\\_transactions.csv -Delimiter ; -DecimalPoint ,"
if [ $? -ne 0 ]; then aborttest_parse "silk convert tsd to csv"; return; fi

sleep 5

echo Parse transactiondata to intermediate...
$rpt_toolspath/srt.parsetransactions.exe parser=silkperformer transactionfilebrp=$rpt_temppath/_transactions.brp transactionfilecsv=$rpt_temppath/_transactions.csv intermediatefile=$rpt_temppath/_intermediate.trs.csv
if [ $? -ne 0 ]; then aborttest_parse "parse transactions"; return; fi

echo Parse measures to intermediate...
$rpt_toolspath/srt.parsemeasures.exe parser=silkperformer transactionfilecsv=$rpt_temppath/_transactions.csv intermediatefile=$rpt_temppath/_intermediate.msr.csv
if [ $? -ne 0 ]; then aborttest_parse "parse measures"; return; fi

echo Parse variables to intermediate...
$rpt_toolspath/srt.parsevariables.exe parser=silkperformer transactionfilecsv=$rpt_temppath/_transactions.csv transactionfilebrp=$rpt_temppath/_transactions.brp intermediatefile=$rpt_temppath/_intermediate.var.csv
if [ $? -ne 0 ]; then aborttest_parse "parse variables"; return; fi

echo "Parse [Silkperformer] END"
