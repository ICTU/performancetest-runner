using System.Collections.Generic;

namespace rpg.common
{
    /// <summary>
    /// Generate colorcodes for diff(erence) evaluation
    /// </summary>
    public class Diff
    {
        /// <summary>
        /// Generate colorcode entries based on diff evaluation of all intermediate value entities
        /// input:
        /// key1=1;2;3;blaat;zee
        /// key2=1;3;3;blaat;zoo
        /// ...
        /// output (generated keys):
        /// key1_c="";"highlite";"";"highlite"
        /// key2_c="";"highlite";"";"highlite"
        /// ...
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="colorCode"></param>
        /// <returns></returns>
        public Intermediate GenerateDiffValues(Intermediate intermediate, string colorCode)
        {
            Intermediate result = new Intermediate();

            // controleer occurence in overige intermediate
            foreach (KeyValuePair<string, string> pair in intermediate)
                result.Add(pair.Key + "_c", GetDiffValue(pair, intermediate, colorCode));
            
            return result;
        }

        /// <summary>
        /// Generate colorcode string for this key
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="intermediate"></param>
        /// <param name="markColorCode"></param>
        /// <returns></returns>
        private string GetDiffValue(KeyValuePair<string, string> reference, Intermediate intermediate, string markColorCode)
        {
            List<string> colValues = new List<string>();

            // itereer over alle value items in values string
            foreach (string refValueItem in reference.Value.Split(Intermediate.LISTSEPARATOR) )
            {
                if ( EvaluateDiffValueItem(reference.Key, refValueItem, intermediate) )
                    colValues.Add("");
                else
                {
                    Log.WriteLine(string.Format("add diffmark for {0}/{1}", reference.Key, refValueItem));
                    colValues.Add(markColorCode);
                }
            }
            // plak alle waarden weer in 1 string, listeparator gescheiden
            return string.Join(Intermediate.LISTSEPARATOR.ToString(), colValues.ToArray());
        }

        /// <summary>
        /// Compware value to all other value strings, return true if corresponding value is found
        /// </summary>
        /// <param name="refKey"></param>
        /// <param name="refValueItem"></param>
        /// <param name="intermediate"></param>
        /// <returns></returns>
        private bool EvaluateDiffValueItem(string refKey, string refValueItem, Intermediate intermediate)
        {
            foreach (KeyValuePair<string, string> item in intermediate)
            {
                // zichzelf overslaan
                if (item.Key != refKey)
                {
                    // algoritme 1: zoek broertje of zusje in alle value items van alle keys in de intermediate
                    // true als gevonden
                    foreach (string evalValueItem in item.Value.Split(Intermediate.LISTSEPARATOR))
                    {
                        if (evalValueItem == refValueItem)
                            return true;
                    }
                }
            }
            return false;
        }




    }
}
