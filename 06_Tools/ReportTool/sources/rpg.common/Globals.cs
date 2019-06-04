using System;
using System.Collections.Generic;
using System.Text;

namespace rpg.common
{
    /// <summary>
    /// Global static variables
    /// </summary>
    public static class Globals
    {
        public static string dbconnectstring = ""; // format: "server:port:username:password"

        public static char decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]; // <summary> global decimal separator (system wide) </summary>

    }
}
