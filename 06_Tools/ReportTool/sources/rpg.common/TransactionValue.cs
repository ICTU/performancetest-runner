using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpg.common
{
    /// <summary>
    /// Wrapper for handling transaction values
    /// </summary>
    public class TransactionValue
    {
        /// <summary>Separating values in the list</summary>
        public static char LISTSEPARATOR = ';';
        public string AGGREGATE_FLOATFORMAT = "0.000";

        /// <summary>Raw value list </summary>
        public string[] raw = new string[10];

        /// <summary>Fieldnames, letop: deze opsomming gelijk houden aan veldvolgorde!</summary>
        public static string[] fieldnames = {"count","minimum","average","maximum","90 percentile","failed","canceled","median","95 percentile","std deviation"};

        /// <summary> index of 'cnt' </summary>
        public int idx_cnt = 0;
        /// <summary> index of 'min'  </summary>
        public int idx_min = 1;
        /// <summary> index of 'avg' </summary>
        public int idx_avg = 2;
        /// <summary> index of 'max' </summary>
        public int idx_max = 3;
        /// <summary> index of 'p90' </summary>
        public int idx_p90 = 4;
        /// <summary> index of 'fail' </summary>
        public int idx_fail = 5;
        /// <summary> index of 'cancel' </summary>
        public int idx_cancel = 6;
        /// <summary> index of 'median' </summary>
        public int idx_median = 7;
        /// <summary> index of 'p95' </summary>
        public int idx_p95 = 8;
        /// <summary> index of 'stddev' </summary>
        public int idx_stdev = 9;

        /// <summary> cnt = count  </summary>
        public string cnt { get { return raw[idx_cnt]; } set { raw[idx_cnt] = value; } }
        /// <summary> min = minimum value </summary>
        public string min { get { return raw[idx_min]; } set { raw[idx_min] = value; } }
        /// <summary> avg = average value </summary>
        public string avg { get { return raw[idx_avg]; } set { raw[idx_avg] = value; } }
        /// <summary> max = maximum value </summary>
        public string max { get { return raw[idx_max]; } set { raw[idx_max] = value; } }
        /// <summary> p90 = 90th percentile </summary>
        public string p90 { get { return raw[idx_p90]; } set { raw[idx_p90] = value; } }
        /// <summary> fail = num of failed </summary>
        public string fail { get { return raw[idx_fail]; } set { raw[idx_fail] = value; } }
        /// <summary> cancel = num of canceled </summary>
        public string cancel { get { return raw[idx_cancel]; } set { raw[idx_cancel] = value; } }
        /// <summary> median = 50th/median percentile </summary>
        public string median { get { return raw[idx_median]; } set { raw[idx_median] = value; } }
        /// <summary> p95 = 95th percentile </summary>
        public string p95 { get { return raw[idx_p95]; } set { raw[idx_p95] = value; } }
        /// <summary> stddev = atandard Deviation </summary>
        public string stdev { get { return raw[idx_stdev]; } set { raw[idx_stdev] = value; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransactionValue()
        {
            cnt = "0";
        }

        /// <summary>
        /// Parse list of values to local values
        /// </summary>
        /// <param name="value"></param>
        public TransactionValue(string value)
        {
            string[] arr = value.Split(LISTSEPARATOR);
            try { cnt = arr[0]; }
            catch { };
            try { min = arr[1]; }
            catch { };
            try { avg = arr[2]; }
            catch { };
            try { max = arr[3]; }
            catch { };
            try { p90 = arr[4]; }
            catch { };
            try { fail = arr[5]; }
            catch { };
            try { cancel = arr[6]; }
            catch { };
            try { median = arr[7]; }
            catch { };
            try { p95 = arr[8]; }
            catch { };
            try { stdev = arr[9]; }
            catch { };
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns></returns>
        public static string CreateEmptyValue()
        {
            return new TransactionValue().ToString();
        }

        /// <summary>
        /// Represent as string (value)
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return string.Join(LISTSEPARATOR.ToString(), raw);
        }

    }

    /// <summary>
    /// Wrapper for handling transaction aggregates
    /// </summary>
    public class TransactionValueAggregate: TransactionValue
    {
        private Int32 eval_cnt = 0;

        private Int32 raw_cnt = 0;
        private double raw_min = double.MaxValue;
        private double raw_avg = 0;
        private double raw_max = 0;
        private double raw_p90 = 0;
        private Int32 raw_fail = 0;
        private Int32 raw_cancel = 0;
        private double raw_median = 0;
        private double raw_p95 = 0;
        private double raw_stdev = 0;
        
        /// <summary>
        /// Convert string to integer and add to baseValue, return as integer
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="addValue"></param>
        /// <returns></returns>
        private Int32 AddStrInt(Int32 baseValue, string addValue)
        {
            try
            {
                Int32 i = Int32.Parse(addValue);
                return baseValue + i;
            }
            catch
            {
                return baseValue;
            }
        }

        /// <summary>
        /// Return lowest value as double
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private double LowestStrDouble(double baseValue, string value)
        {
            try
            {
                double d = double.Parse(Utils.ToSystemFloatString(value));
                return d < baseValue ? d : baseValue;
            }
            catch
            {
                return baseValue;
            }
        }

        /// <summary>
        /// Return added values
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private double AddStrDouble(double baseValue, string value)
        {
            try
            {
                double d = double.Parse(Utils.ToSystemFloatString(value));
                return baseValue + d;
            }
            catch
            {
                return baseValue;
            }
        }

        private double HighestStrDouble(double baseValue, string value)
        {
            try
            {
                double d = double.Parse(Utils.ToSystemFloatString(value));
                return baseValue > d ? baseValue : d;
            }
            catch
            {
                return baseValue;
            }
        }

        /// <summary>
        /// Evaluate all values for aggregation purposes
        /// </summary>
        /// <param name="value"></param>
        public void AddTransaction(TransactionValue value)
        {
            eval_cnt++; // num of evaluated transactions
            raw_cnt = AddStrInt(raw_cnt, value.cnt);
            raw_min = LowestStrDouble(raw_min, value.min);
            raw_avg = AddStrDouble(raw_avg, value.avg);
            raw_max = HighestStrDouble(raw_max, value.max);
            raw_median = AddStrDouble(raw_median, value.median);
            raw_p90 = AddStrDouble(raw_p90, value.p90);
            raw_p95 = AddStrDouble(raw_p95, value.p95);
            raw_fail = AddStrInt(raw_fail, value.fail);
            raw_cancel = AddStrInt(raw_cancel, value.cancel);
            raw_stdev = AddStrDouble(raw_stdev, value.stdev);
        }

        /// <summary>
        /// Conclude aggregate evaluation
        /// </summary>
        public void Aggregate()
        {
            cnt = raw_cnt.ToString(); // cnt
            min = raw_min.ToString(); // min of min
            avg = (raw_avg / eval_cnt).ToString(AGGREGATE_FLOATFORMAT); // average of average
            max = raw_max.ToString(); // max of max
            median = (raw_median / eval_cnt).ToString(AGGREGATE_FLOATFORMAT); // average of percentile
            p90 = (raw_p90 / eval_cnt).ToString(AGGREGATE_FLOATFORMAT); // average of percentile
            p95 = (raw_p95 / eval_cnt).ToString(AGGREGATE_FLOATFORMAT); // average of percentile
            fail = raw_fail.ToString();
            cancel = raw_cancel.ToString();
            stdev = (raw_stdev / eval_cnt).ToString(AGGREGATE_FLOATFORMAT);
        }
    }
}
