using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace rpg.common
{
    /// <summary>
    /// Handle baselining
    /// </summary>
    public class Baselining
    {
        /// <summary>
        /// string marking below threshold
        /// </summary>
        public static string belowLowThreshold = "belowLowThreshold";

        /// <summary>
        /// Number of baseline alerts for this testrun
        /// </summary>
        public string BASELINEALERTCOUNTVARNAME = "_baselinealertcount";


        /// <summary>
        /// Generate new Intermediate with baselining values
        /// ${Appbeheer_01_Inloggen_br // reference (baseline value)
        /// ${Appbeheer_01_Inloggen_be // evaluated value (difference from baseline in %)
        /// ${Appbeheer_01_Inloggen_be_c // colorcode, mark when current value exceeds threshold th1, green when diff > -15%, red when diff > +15%
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="baselineIntermediate"></param>
        /// <param name="baselineReferenceIntermediate"></param>
        /// <param name="colorcodeBetter"></param>
        /// <param name="colorcodeWorse"></param>
        /// <returns></returns>
        public Intermediate GenerateBaselineEvaluationValues(Intermediate intermediate, Intermediate baselineIntermediate, Intermediate baselineReferenceIntermediate, string colorcodeBetter, string colorcodeWorse)
        {
            Intermediate result = new Intermediate();

            // voor alle items in de baseline reference: doe evaluatie
            foreach (KeyValuePair<string, string> baselinePair in baselineReferenceIntermediate)
            {
                // add _br value from baseline reference
                result.AddValue( GetBaselineReferenceKey(baselinePair.Key), baselinePair.Value);

                // evaluate and generate _be and _be_c values
                if (intermediate.ContainsKey(baselinePair.Key))
                {
                    string baselineValueSeries = baselineIntermediate.GetValue(baselinePair.Key);
                    string baselineThresholdValueSeries = baselineIntermediate.GetValue(Thresholds.GetThresholdKey(baselinePair.Key));

                    string currentValueSeries = intermediate.GetValue(baselinePair.Key);
                    string currentThresholdValueSeries = intermediate.GetValue(Thresholds.GetThresholdKey(baselinePair.Key));

                    // generate _be and _be_c values
                    Intermediate evalResult = GenerateEvaluation(baselinePair.Key, baselineValueSeries, currentValueSeries, baselineThresholdValueSeries, currentThresholdValueSeries, colorcodeBetter, colorcodeWorse);
                    result.Add( evalResult );
                }
            }

            return result;
        }

        /// <summary>
        /// Generate _be and _be_c values (baseline evaluation and colorcodes)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="baselineValueSeries"></param>
        /// <param name="currentValueSeries"></param>
        /// <param name="baselineThresholdValueSeries"></param>
        /// <param name="currentThresholdValueSeries"></param>
        /// <param name="colorcodeBetter"></param>
        /// <param name="colorcodeWorse"></param>
        /// <returns></returns>
        private Intermediate GenerateEvaluation(string key, string baselineValueSeries, string currentValueSeries, string baselineThresholdValueSeries, string currentThresholdValueSeries, string colorcodeBetter, string colorcodeWorse)
        {
            Intermediate result = new Intermediate();

            string[] baselineValueArr = baselineValueSeries.Split(Intermediate.LISTSEPARATOR);
            string[] baselineThresholdValueArr = baselineThresholdValueSeries.Split(Intermediate.LISTSEPARATOR);

            string[] currentValueArr = currentValueSeries.Split(Intermediate.LISTSEPARATOR);
            string[] currentThresholdValueArr = currentThresholdValueSeries.Split(Intermediate.LISTSEPARATOR);

            List<string> beValues = new List<string>();
            List<string> becValues = new List<string>();

            Log.Write(string.Format("baseline evaluation triggers for {0}: [", key));
            for (int i = 0; i < baselineValueArr.Length ; i++)
            {
                try
                {
                    // only perform evaluation if current or baseline value is other than green
                    if ((currentThresholdValueArr[i] != Baselining.belowLowThreshold) || (baselineThresholdValueArr[i] != Baselining.belowLowThreshold))
                    {
                        //Log.WriteLine(string.Format("baseline evaluation trigger: current or baseline exceeding non-green level on {0}", key));

                        // add evaluation to beValues and becValues
                        DoEvaluate(baselineValueArr[i], currentValueArr[i], beValues, becValues, colorcodeBetter, colorcodeWorse);
                    }
                    else
                    {
                        beValues.Add(""); // no value evaluation
                        becValues.Add(""); // no colorcode
                    }
                }
                catch
                {
                    beValues.Add(""); // no value evaluation
                    becValues.Add(""); // no colorcode
                }

            }
            Log.WriteLine("]");

            // values
            result.Add(GetBaselineEvaluationKey(key), string.Join(Intermediate.LISTSEPARATOR.ToString(), beValues.ToArray()) );

            // colorcodes
            result.Add(GetBaselineEvaluationColorKey(key), string.Join(Intermediate.LISTSEPARATOR.ToString(), becValues.ToArray()) );

            return result;
        }

        /// <summary>
        /// Perform the evaluation
        /// </summary>
        /// <param name="baselineValueStr"></param>
        /// <param name="evaluateValueStr"></param>
        /// <param name="beValues"></param>
        /// <param name="becValues"></param>
        /// <param name="colorcodeBetter"></param>
        /// <param name="colorcodeWorse"></param>
        private void DoEvaluate(string baselineValueStr, string evaluateValueStr, List<string> beValues, List<string> becValues, string colorcodeBetter, string colorcodeWorse)
        {
            string evalStr = "";
            string colorCode = "";

            try
            {
                double baselineValue = Thresholds.StringValueToDouble(baselineValueStr);
                double evaluateValue = Thresholds.StringValueToDouble(evaluateValueStr);


                // give up if input is nonsense
                if (!double.IsNaN(baselineValue) && !double.IsNaN(evaluateValue))
                {
                    //Log.Write("DEBUG baseline evaluation: ");

                    double evalValue = 100 * ((evaluateValue - baselineValue) / baselineValue);
                    //Log.Write(string.Format("curvalue={0} baselinevalue={1} delta={2} ", evaluateValue, baselineValue, evalValue));

                    // only color value if overshoot > +-15%
                    colorCode = (evalValue > 15) ? colorcodeWorse : (evalValue < -15) ? colorcodeBetter : string.Empty;

                    if (colorCode != string.Empty)
                    {
                        Log.Write(".");
                        //evalStr = string.Format("{0:+0.0;-0.0;0}%", evalValue);                        
                        evalStr = string.Format("{0:+0.0;-0.0;0}", evalValue); // format to intermediate standard
                    }

                    //Log.WriteLine("deltaformatted=" + evalStr);
                }
            }
            catch (Exception)
            { }

            // always add value
            beValues.Add(evalStr);
            becValues.Add(colorCode);
        }

        /// <summary>
        /// Generate key for baseline reference value
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        public static string GetBaselineReferenceKey(string orgKey)
        {
            return orgKey + "_br";
        }

        /// <summary>
        /// Generate key for baseline comparison value
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        public static string GetBaselineEvaluationKey(string orgKey)
        {
            return orgKey + "_be";
        }

        /// <summary>
        /// Generate key for color code the baseline comparison value
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        public static string GetBaselineEvaluationColorKey(string orgKey)
        {
            return orgKey + "_be_c";
        }

    }
}
