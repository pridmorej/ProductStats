using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProductStats.Models;

namespace ProductStats.Services
{
    public class ProductTickerVM
    {
        public ProductTickerVM(ProductTicker productTicker)
        {
            this.ProductTicker = productTicker;
        }

        public ProductTicker ProductTicker { get; private set; }

        public bool HasOpenPositions { get; set; }
    }
}
