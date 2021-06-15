using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductStats.Models
{
    public class Rate
    {
        /// <summary>
        /// The DateTime at which this Rate is valid.
        /// </summary>
        public DateTime Timestamp;

        /// <summary>
        /// The Product to which this Rate relates.
        /// </summary>
        public string ProductId;

        /// <summary>
        /// The lowest price a seller will accept when I am buying.
        /// </summary>
        public decimal Ask;

        /// <summary>
        /// The highest price a buyer will pay when I am selling.
        /// </summary>
        public decimal Bid;
    }
}
