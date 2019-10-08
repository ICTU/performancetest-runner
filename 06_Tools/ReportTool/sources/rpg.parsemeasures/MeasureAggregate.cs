using rpg.common;

namespace rpg.parsemeasures
{
    class MeasureAggregate
    {
        long sum = 0;
        long count = 0;
        long min = 0;
        long max = 0;

        /// <summary>
        /// Add integer value
        /// </summary>
        /// <param name="value"></param>
        public void Add(long value)
        {
            sum += value;

            if (value > max)
                max = value;

            if (count == 0)
                min = value;
            if (value < min)
                min = value;
            count++;
        }

        /// <summary>
        /// Get over-all average
        /// </summary>
        /// <returns></returns>
        public long Avg()
        {
            try
            {
                return sum / count;
            }
            catch
            {
                //Log.WriteLine("WARNING: no sample count, cannot aggregate average");
                return 0;
            }
        }

        /// <summary>
        /// Get over-all max
        /// </summary>
        /// <returns></returns>
        public long Max()
        {
            return max;
        }

        /// <summary>
        /// Get over-all min
        /// </summary>
        /// <returns></returns>
        public long Min()
        {
            return min;
        }

        /// <summary>
        /// Get total count
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            return count;
        }

        /// <summary>
        /// Get sum of values
        /// </summary>
        /// <returns></returns>
        public long Sum()
        {
            return sum;
        }

        /// <summary>
        /// Reset of internal counters
        /// </summary>
        internal void Reset()
        {
            sum = 0;
            count = 0;
            min = 0;
            max = 0;
        }
    }
}
