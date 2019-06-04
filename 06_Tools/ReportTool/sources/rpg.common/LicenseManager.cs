using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpg.common
{
    /// <summary>
    /// Licentsemanager, for commercial use only
    /// </summary>
    public class LicenseManager
    {
        /// <summary>
        /// Date of expiration of current license
        /// </summary>
        public DateTime ExpirationDate
        {
            get { return new DateTime(2017, 1, 1); }
        }

        /// <summary>
        /// Date of expiration of current license (as string)
        /// </summary>
        public string ExpirationDateStr
        {
            get { return this.ExpirationDate.ToString("dd-MM-yyyy"); }
        }

        /// <summary>
        /// Is current license valid?
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            //return (this.ExpirationDate > DateTime.Now);
            return true;
        }

        /// <summary>
        /// Get HTML text expired
        /// </summary>
        /// <returns></returns>
        public string GetHtmlBoldExpiredSince()
        {
            return String.Format("<table><tr><b>Reporting tool right of use expired since {0}, please contact Ymor support</b><br>", ExpirationDateStr);
        }
    }
}
