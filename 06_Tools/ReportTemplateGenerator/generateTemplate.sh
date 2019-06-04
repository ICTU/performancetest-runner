#!/bin/bash
# --------
# 2017-nov-03 V1: Eerste versie van het script
# --------
# Dit script heeft als doel het genereren van een report template aan de hand van een transactiefile
# Het script verwacht als eerste parameter de afkorting naam van het project
# Het script verwacht als tweede parameter de locatie van de Transaction file
# --------

# Inkomende variabelen
#------------------------------------------------------------
projectNaam=$1
Transactions=$2
if [[ -z "$projectNaam" ]]; then
	echo "ProjectNaam niet ingevuld, script stopt nu"
	exit
fi
if [[ -z "$Transactions" ]]; then
	echo "Transactions file niet meegeleverd, script stopt nu"
	exit
else
	if [ ! -f $Transactions ]; then
    	echo "Transactie bestandnaam wel meegeleverd maar kan niet gevonden worden! script stopt nu"
    	exit
	fi
fi
#------------------------------------------------------------

# Aanmaken templates folder
#------------------------------------------------------------
mkdir -p ./templates
#------------------------------------------------------------

# Functies
#------------------------------------------------------------
removeIfExists() {
	#echo "Check if $1 exists"
	if [ -f $1 ] ; then 
		echo "Removing $1"
		rm -f $1
	fi
}

genereerVergelijkGrafiek(){
	while read p; do
		pn=$(echo $p | sed -e 's/\r//g') # verwijder de regel einde uit TransactionFile
		ps=$(echo $pn | sed -e 's/\&/\\&/g') # Zet een \ voor & tekens (ivm escapen van special characters)
		if [ "$pn" = "HEADER" ]; then
			echo "Doe niks" > /dev/null
		else
			# Vervang ||TransactionName|| met transactienaam in het vergelijkGrafiek blok
			sed "s/||TransactionName||/$ps/" < blok_grafiek_1 >> intermediate_VergelijkGrafiek_1
		fi
	done <$Transactions

	# Plaats comma tussen de blokken (zo gedaan anders last van laatste regel waar geen comma moet)
	sed "s/}{/},{/" < intermediate_VergelijkGrafiek_1 > intermediate_VergelijkGrafiek_2
}

genereerDeltaTabel(){
	while read p; do
		pn=$(echo $p | sed -e 's/\r//g') # verwijder de regel einde uit TransactionFile
		ps=$(echo $pn | sed -e 's/\&/\\&/g') # Zet een \ voor & tekens (ivm escapen van special characters)
		if [ "$pn" = "HEADER" ]; then
			cat blok_deltaTabel_2 >> intermediate_Delta_1
		else
			# Vervang ||TransactionName|| met transactienaam in het deltaTabel blok
			sed "s/||TransactionName||/$ps/g" < blok_deltaTabel_1 >> intermediate_Delta_1
		fi
	done <$Transactions
}

genereerTrendTabel(){
	while read p; do
		pn=$(echo $p | sed -e 's/\r//g') # verwijder de regel einde uit TransactionFile
		ps=$(echo $pn | sed -e 's/\&/\\&/g') # Zet een \ voor & tekens (ivm escapen van special characters)
		if [ "$pn" = "HEADER" ]; then
			cat blok_TrendTabel_2 >> intermediate_blokTrend_1
		else
			# Vervang ||TransactionName|| met transactienaam in het trendTabel blok
			sed "s/||TransactionName||/$ps/g" < blok_TrendTabel_1 >> intermediate_blokTrend_1
		fi
	done <$Transactions
}

vervangInTemplate(){
	intermediate_grafiek=$(<intermediate_VergelijkGrafiek_2)
	intermediate_deltaTabel=$(<intermediate_Delta_1)
	intermediate_trendTabel=$(<intermediate_blokTrend_1)
	
	intermediate_final_1=$(<report_template.html)
	echo "${intermediate_final_1//||BLOK1||/$intermediate_grafiek}" > intermediate_final_2

	intermediate_final_2=$(<intermediate_final_2)
	echo "${intermediate_final_2//||BLOK2||/$intermediate_deltaTabel}" > intermediate_final_3

	intermediate_final_3=$(<intermediate_final_3)
	echo "${intermediate_final_3//||BLOK3||/$intermediate_trendTabel}" > intermediate_final
}

vervangProjectNaamEnVerplaats(){
	sed "s/||Project||/$projectNaam/g" < intermediate_final > ./templates/"$projectNaam"_report_template.html
}

removeDOSCarriageReturn(){
	sed -e 's/\r$//' $Transactions > ${Transactions}.tmp && mv -f ${Transactions}.tmp $Transactions
}

#------------------------------------------------------------

# begin met verwijderen van oude bestanden
echo "====="
echo "Start with removing temp files"
removeIfExists intermediate_VergelijkGrafiek_1
removeIfExists intermediate_VergelijkGrafiek_2
removeIfExists intermediate_Delta_1
removeIfExists intermediate_blokTrend_1
removeIfExists intermediate_output_1
removeIfExists intermediate_output_2
removeIfExists intermediate_output_3
removeIfExists intermediate_final
removeIfExists intermediate_final_1
removeIfExists intermediate_final_2
removeIfExists intermediate_final_3
removeIfExists /templates/"$projectNaam"_report_template.html
echo "Done with removing temp files"
echo "---"
echo "Start with removeDOSCarriageReturn"
removeDOSCarriageReturn
echo "End with removeDOSCarriageReturn"
echo "---"
echo "Start with generating parts"
genereerVergelijkGrafiek
genereerDeltaTabel
genereerTrendTabel
echo "Done with generating parts"
echo "---"
echo "Start generate template and subsitute the parts"
vervangInTemplate
echo "Done generate template and subsitute the parts"
echo "---"
echo "Start replacing the projectname"
vervangProjectNaamEnVerplaats
echo "Done replacing the projectname"
echo "====="