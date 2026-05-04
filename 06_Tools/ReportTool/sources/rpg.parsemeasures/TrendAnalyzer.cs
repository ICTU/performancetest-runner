using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;
using rpg.common;

namespace rpg.parsemeasures
{
    /// <summary>
    /// First in first out stack class
    /// </summary>
    public class FifoStackDouble
    {
        public List<double> Elements = null;
        private int maxElements = 0;

        public FifoStackDouble(int numOfElements)
        {
            Elements = new List<double>();
            maxElements = numOfElements;
        }

        /// <summary>
        /// Add new entry (fifo)
        /// </summary>
        /// <param name="value"></param>
        public void Put(double value)
        {
            Elements.Add(value);
            Pack();
        }

        /// <summary>
        /// Remove old entries
        /// </summary>
        public void Pack()
        {
            while (Elements.Count > maxElements)
                Elements.RemoveAt(0);
        }

        public int NumOfElements()
        {
            return Elements.Count;
        }

        /// <summary>
        /// Retrieve [index] value
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double Get(int index)
        {
            return Elements[index];
        }

        /// <summary>
        /// Get sum of all values in stack
        /// </summary>
        /// <returns></returns>
        public double GetSum()
        {
            double result = 0;
            foreach (double val in Elements)
                result += val;
            return result;
        }

    }

    /// <summary>
    /// Perform calculations needed to determine trend analysis
    /// </summary>
    public class TrendAnalyzer
    {
        public double[] ReferenceSeries;

        private int _breakIndex = -1;

        // trendbreak stability/endurance parameters
        private int VARIATIONPERCENTAGE_STABILITY = 25; // percent fluctuation threshold
        private int ELEMENTSEVALUATED_STABILITY = 5; // number of historic values evaluated for values
        private int ELEMENTSEVALUATED_STABILITY_BAND = 5; // number of historic values evaluated for the band
        //<add key = "TrenddetectionStabilityVariationpercentage" value = "25" />
        //<add key = "TrenddetectionStabilityElementsevaluated" value = "5" />

        // trendbreak rampup/stress
        private int VARIATIONFACTOR_RAMPUP = 5; // factor x drop of expected difference
        private int ELEMENTSEVALUATED_RAMPUP = 10; // number of historic values evaluated for moving windows of averages
        private int ELEMENTSSTART_RAMPUP = 30; // number of values to start of baseline
        private int ELEMENTSSPAN_RAMPUP = 30; // number of values to span the baseline (1st 5 mins)
        private float BREAKFACTOR_RAMPUP = 2; // factor times reference level
        //<add key = "TrenddetectionRampupVariationfactor" value = "5" />
        //<add key = "TrenddetectionRampupElementsevaluated" value = "10" />
        //<add key = "TrenddetectionRampupReferenceelementsstart" value = "30" />
        //<add key = "TrenddetectionRampupReferenceelementsspan" value = "30" />
        //<add key = "TrenddetectionRampupBreakfactor" value = "2" />

        public int BreakIndex
        {
            get { return _breakIndex; }
            set { _breakIndex = value; }
        }
        
        /// <summary>
        /// Get the breaking value factor relative to max
        /// </summary>
        /// <returns></returns>
        public double GetBreakFactor_Value()
        {
            double breakReferenceValue = ReferenceSeries[_breakIndex];
            double maxReferenceValue = ReferenceSeries.Max();
            
            return (double)breakReferenceValue / maxReferenceValue;
        }

        /// <summary>
        /// Get the breaking value, if end of scale: max of the range
        /// </summary>
        /// <param name="CorrectForMax">search for max value if breakIndex is end of scale</param>
        /// <returns></returns>
        public double GetBreakValue_Value(bool CorrectForMax = false)
        {
            double breakReferenceValue = ReferenceSeries[_breakIndex];

            if ((CorrectForMax) && (_breakIndex == ReferenceSeries.Length-1))
                breakReferenceValue = ReferenceSeries.Max();

            return (double)breakReferenceValue;
        }

        /// <summary>
        /// Get the breaking value factor relative to max
        /// </summary>
        /// <returns></returns>
        public double GetBreakFactor_Progress()
        {
            double breakReferenceValue = _breakIndex;
            double maxReferenceValue = ReferenceSeries.Length - 1;

            return (double)breakReferenceValue / maxReferenceValue;
        }

        /// <summary>
        /// Get percentage break relative to 100% progress
        /// </summary>
        /// <returns></returns>
        public double GetBreakPercentage_Progress()
        {
            return GetBreakFactor_Progress() * 100;
        }

        /// <summary>
        /// Get percentage break relative to max
        /// </summary>
        /// <returns></returns>
        public double GetBreakPercentage_Reference()
        {
            return GetBreakFactor_Value() * 100;
        }

        /// <summary>
        /// Get value of reference series at which break occured
        /// </summary>
        /// <returns></returns>
        public double GetBreakReferenceValue()
        {
            return GetBreakValue_Value(true);
        }

        /// <summary>
        /// Detect trendbreak based on band of previous x values
        /// </summary>
        /// <param name="values"></param>
        /// <returns>array index of breakpoint</returns>
        public int DetectTrendBreak_Stability_HistoricalBand(double[] values)
        {
            Log.WriteLine("detect trendbreak-stability [historicalband] started...");

            // get new array with variable band (high, low)
            double[,] bandValues = GenerateBandArray(values, ELEMENTSEVALUATED_STABILITY_BAND);

            // evaluate values against flexible band, allowing for variationpercentage margin
            _breakIndex = DetectHistoricalBandBreak(bandValues, values, VARIATIONPERCENTAGE_STABILITY);

            // evaluate trend and consider breaking if value is out of expected bandwidth
            //_breakIndex = DetectBandBreak(avgValues, values, ELEMENTSEVALUATED_STABILITY, VARIATIONPERCENTAGE_STABILITY);
            Log.WriteLine(string.Format("break found at value={0:0.0} referenceseries={1:0.0}", values[_breakIndex], ReferenceSeries[_breakIndex]));

            return _breakIndex;
        }


        /// <summary>
        /// Detect trendbreak according to previous avg values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public int DetectTrendBreak_Stability_FixedBand(double[] values)
        {
            Log.WriteLine("detect trendbreak-stability [fixed band] started...");
            double[] avgValues = GenerateAvgArray(values, ELEMENTSEVALUATED_STABILITY);

            // evaluate trend and consider breaking if value is out of expected bandwidth
            _breakIndex = DetectBandBreak(avgValues, values, ELEMENTSEVALUATED_STABILITY, VARIATIONPERCENTAGE_STABILITY);
            Log.WriteLine(string.Format("break found at value={0:0.0} referenceseries={1:0.0}", values[_breakIndex], ReferenceSeries[_breakIndex]));

            return _breakIndex;
        }

        /// <summary>
        /// Mark breakpoint where current value breaks the prepared band-with-margin
        /// </summary>
        /// <param name="bandValues"></param>
        /// <param name="values"></param>
        /// <returns>index</returns>
        private int DetectHistoricalBandBreak(double[,] bandValues, double[] values, int variationPercentageStability)
        {
            int result = -1;

            for (int iValue = 1; iValue < values.Length; iValue++)
            {
                int iBand = iValue - 1;
                double variance = (bandValues[iBand, 2] - bandValues[iBand, 1]) / 100 * variationPercentageStability;

                if ((values[iValue] < bandValues[iBand, 1] - variance) || (values[iValue] > bandValues[iBand, 2] + variance))
                {
                    result = iValue;
                    break;
                }
            }

            if (result == -1)
            {
                Log.WriteLine("No break found, set result to 100%");
                result = values.Length - 1;
            }

            return result;
        }

        /// <summary>
        /// Generate a measurement history reference band
        /// </summary>
        /// <param name="values"></param>
        /// <param name="eLEMENTSEVALUATED_STABILITY_BAND"></param>
        /// <param name="vARIATIONPERCENTAGE_STABILITY"></param>
        /// <returns>index</returns>
        private double[,] GenerateBandArray(double[] values, int elementsEvaluatedStabilityBand)
        {
            double[,] band = new double[values.Length, 2];

            // take the whole range of values
            for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                double minValue = values[valueIndex];
                double maxValue = values[valueIndex];

                // take min and max from history (elementsEvaluatedStabilityBand) up to valueIndex
                for (int historyIndex = valueIndex - elementsEvaluatedStabilityBand; historyIndex <= valueIndex; historyIndex++)
                {
                    var bandIndex = (historyIndex < 0) ? valueIndex : historyIndex;

                    if (values[bandIndex] < minValue) minValue = values[bandIndex];
                    if (values[bandIndex] > maxValue) maxValue = values[bandIndex];
                }

                band[valueIndex, 1] = minValue;
                band[valueIndex, 2] = maxValue;
            }
            return band;
        }

        /// <summary>
        /// Detect the maximum variance (diff between lower and upper values)
        /// </summary>
        /// <param name="avgValues">range of (reference) values</param>
        /// <returns></returns>
        //private double DetectMaxVariance_Percentage(double[] avgValues)
        //{
        //    double former = avgValues[0];
        //    double maxVariance = 0;

        //    for (int i = 1; i < avgValues.Length - 1;  i++)
        //    {
        //        // TODO review (quickfix)
        //        var variance = Math.Abs(avgValues[i] - avgValues[i - 1]) / avgValues[i-1];
        //        maxVariance = variance > maxVariance ? variance : maxVariance;
        //    }
        //    // TODO review (quickfix)
        //    return maxVariance;
        //}

        /// <summary>
        /// Detect a trend break based on expected rise, derived from extrapolation based on 3 point historical average
        ///   used for stress testing (ramp-up)
        /// </summary>
        /// <param name="throughput_values"></param>
        /// <returns></returns>
        public int DetectTrendBreak_Rampup_Throughput(double[] throughput_values)
        {
            Log.WriteLine("Detection of trendbreak rampup started (response time rise trend stability)...");

            double[] avgArray = GenerateAvgArray(throughput_values, ELEMENTSEVALUATED_RAMPUP);

            // evaluate trend and consider breaking if new value is not rising enough
            _breakIndex = DetectRiseBreak(avgArray, throughput_values, ELEMENTSEVALUATED_RAMPUP, VARIATIONFACTOR_RAMPUP);
            Log.WriteLine(string.Format("break found at throughput interval avg={0:0.000} reference={1:0.000}", avgArray[_breakIndex], ReferenceSeries[_breakIndex]));

            return _breakIndex;
        }

        /// <summary>
        /// Detect a break for ramp-up pattern load in response times. Trend break after breaching factor x reference average response times
        /// </summary>
        /// <param name="responsetime_values"></param>
        /// <returns></returns>
        public int DetectTrendBreak_Rampup_Responsetimes(double[] responsetime_values)
        {
            Log.WriteLine("Detection of trendbreak rampup started (response time stability)...");

            // get interval averages from raw response times
            double[] avgArray = GenerateAvgArray(responsetime_values, ELEMENTSEVALUATED_RAMPUP);

            // get average from reference period between elementsstart and elementsspan/window
            double[] referenceArray = GetArraySubset(avgArray, ELEMENTSSTART_RAMPUP, ELEMENTSSPAN_RAMPUP);

            // get grand average from reference period
            double referenceAvg = GetAvgFromSeries(referenceArray);

            // breaks when a level of X * reference is exceeded once, scan starts after referenceperiod (start+span)
            _breakIndex = DetectLevelBreak(avgArray, referenceAvg, BREAKFACTOR_RAMPUP, ELEMENTSSTART_RAMPUP+ELEMENTSSPAN_RAMPUP);
            Log.WriteLine(string.Format("break set at response time interval avg={0:0.000} reference={1:000}", avgArray[_breakIndex], ReferenceSeries[_breakIndex]));

            return _breakIndex;
        }

        /// <summary>
        /// Calculate average value from value array
        /// </summary>
        /// <param name="valueArray"></param>
        /// <returns></returns>
        private double GetAvgFromSeries(double[] valueArray)
        {
            double val = 0;
            foreach (double value in valueArray)
                val += value;

            return (val / valueArray.Length);
        }

        /// <summary>
        /// Detect breaking point where element values break through the reference average by breakfactor
        /// </summary>
        /// <param name="valueArray"></param>
        /// <param name="referenceValue"></param>
        /// <param name="breakFactor">factor*referenceValue is the breaking level</param>
        /// <returns></returns>
        private int DetectLevelBreak(double[] valueArray, double referenceValue, float breakFactor, int elementStartIndex)
        {
            // Average from reference series
            Log.WriteLine(string.Format("DetectLevelBreak: start scanning from element {0} breakfactor {1:0.0}...", elementStartIndex, breakFactor));

            for (int i = elementStartIndex; i < valueArray.Length; i++)
            {
                if (valueArray[i] > (referenceValue * breakFactor))
                {
                    Log.WriteLine("break found!");
                    return i;
                }               
            }
            Log.WriteLine("no break found, set break to 100%");
            return valueArray.Length-1;
        }

        /// <summary>
        /// Copy a subset array out of a lengthy array
        /// </summary>
        /// <param name="valueArray"></param>
        /// <param name="element_start"></param>
        /// <param name="element_span"></param>
        /// <returns></returns>
        private double[] GetArraySubset(double[] valueArray, int element_start, int element_span)
        {
            double[] subset = new double[element_span];

            for (int i = 0; i < element_span; i++)
            {
                subset[i] = valueArray[i + element_start];
            }
            return subset;
        }

        /// <summary>
        /// Build an array of averages based on valueArray values
        ///   each value being average of numofelements values
        /// </summary>
        /// <param name="valueArray"></param>
        /// <param name="numOfElements"></param>
        /// <returns></returns>
        private double[] GenerateAvgArray(double[] valueArray, int numOfElements)
        {
            double[] meanArray = new double[valueArray.Length];
            double val;
            double avg;

            try
            {
                FifoStackDouble stack = new FifoStackDouble(numOfElements);

                // create shadow array with averages
                for (int valueIdx = 0; valueIdx < valueArray.Length; valueIdx++)
                {
                    val = valueArray[valueIdx];
                    stack.Put(val);

                    avg = stack.GetSum() / stack.Elements.Count;
                    meanArray[valueIdx] = avg;
                }

            }
            catch (Exception)
            {
                Log.WriteLine("WARNING build avg array failed");
                throw;
            }

            return meanArray;
        }


        /// <summary>
        /// Return index at breakpoint
        /// </summary>
        /// <param name="meanArray"></param>
        /// <param name="values"></param>
        /// <param name="variationPercentage"></param>
        /// <returns></returns>
        private int DetectBandBreak(double[] meanArray, double[] values, int meanEvaluatedElements, int variationPercentage)
        {
            Log.WriteLine(string.Format("DetectBandBreak: delta variation factor={0} evaluated history elements={1} ...", variationPercentage, meanEvaluatedElements));

            int result = -1;
            double lowLimit = 0;
            double highLimit = 0;

            // skip first 10% of samples
            double d = (values.Length / 10); 
            int startIndex = (int)d;
            startIndex = startIndex > 0 ? startIndex : 1;

            // evaluate all samples in the values array
            for (int idx = startIndex; idx < values.Length; idx++)
            {
                // threshold is percentage +/- of expected value (avg)
                double delta = meanArray[idx - 1]  * variationPercentage / 100;

                lowLimit = meanArray[idx - 1] - delta;
                highLimit = meanArray[idx - 1] + delta;

                //Log.Write(".");
                //Log.WriteLine(string.Format("evaluate idx={0} value={1} length={2}", idx, values[idx], values.Length));

                // limit range because peek forward, only evaluate if reasonable number (delta)
                if ((delta > 1) && (idx < values.Length-1))
                {
                    // break if current value breaks floor or ceiling limit
                    bool breakCurrent = (values[idx] < lowLimit) || (values[idx] > highLimit);
                    bool breakNext = (values[idx + 1] < lowLimit) || (values[idx + 1] > highLimit);

                    //Log.WriteLine(string.Format("evaluate {0} {5} (previous={4} delta={3:0} lowlimit={1:0} highlimit={2:0})", values[idx], lowLimit, highLimit, delta, meanArray[idx - 1], values[idx + 1]));

                    if (breakCurrent && breakNext)
                    {
                        //Log.WriteLine();
                        Log.Write(string.Format("Break found: evaluation {0:0.0} {5:0.0} (expected={4:0.0} delta={3:0.0} lowlimit={1:0.0} highlimit={2:0.0})",
                            values[idx], lowLimit, highLimit, delta, meanArray[idx - 1], values[idx + 1]));

                        result = idx - 1;
                        break;
                    }
                }
            }

            if (result == -1)
            {
                Log.WriteLine("No break found, set result to 100%");
                result = values.Length - 1;
            }

            return result;
        }

        /// <summary>
        /// Algorythm for detecting drop in otherwise expected rising trends (stress tests)
        /// </summary>
        /// <param name="meanArray"></param>
        /// <param name="values"></param>
        /// <param name="variationFactor"></param>
        /// <returns></returns>
        private int DetectRiseBreak(double[] meanArray, double[] values, int meanEvaluatedElements, int variationFactor)
        {
            Log.WriteLine(string.Format("ExpectedRiseBreak algorythm with delta variation factor={0} evaluated history elements={1} ...", variationFactor, meanEvaluatedElements));
            int result = -1;

            double delta = 0;
            double expected = 0;
            double lowLimit = 0;

            // skip first 10% of samples
            double d = (values.Length / 10);
            int startIndex = (int)d;
            startIndex = startIndex > 0 ? startIndex : 1;

            // evaluate all samples in the values array
            for (int idx = startIndex; idx < values.Length; idx++)
            {
                // expected delta is previous value - mean of previous x

                delta = Math.Abs(values[idx - 1] - meanArray[idx - 1]) * 2 / meanEvaluatedElements;

                // expected current value is previous value + delta (based on history), based on expected 'ramp-up' pattern
                expected = values[idx - 1] + delta;

                // low threshold is next expected value - delta with variation factor
                lowLimit = expected - (delta * variationFactor);

                //Log.Write(".");

                // break if current value breaks floor limit
                if (values[idx] < lowLimit)
                {
                    //Log.WriteLine();
                    Log.WriteLine(string.Format("Break found: evaluate {0} > {1} (previous={6:0} current={2:0} (delta_expected={3:0} expected={4:0}) lowlimit={5:0})",
                        values[idx], lowLimit, values[idx], delta, expected, lowLimit, values[idx - 1]));

                    // break is definitive if trend is consistent to next values
                    if ((idx < values.Length - 2) && (values[idx + 1] < lowLimit) && (values[idx + 2] < lowLimit))
                    {
                        Log.WriteLine(" BREAK (n+2)");
                        result = idx - 1;
                        break;
                    }
                    //else Log.WriteLine();
                }
            }

            if (result == -1)
            {
                Log.WriteLine("No break found, set result to 100%");
                result = values.Length - 1;
            }

            return result;
        }

    }
}
