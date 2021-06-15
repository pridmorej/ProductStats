using System;
using System.Collections.Generic;
using System.Text;

namespace ProductStats.Models
{
    /// <summary>
    /// Provides details about a Product.
    /// </summary>
    /// <remarks>
    /// Based on a Coinbase class of the similar name.
    /// </remarks>
    public class ProductStat
    {
        public string ProductId { get; set; }

        public DateTime Date { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Last { get; set; }

        public decimal Volume { get; set; }

        public decimal Volume30Day { get; set; }
    }
}
