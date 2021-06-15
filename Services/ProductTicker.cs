using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProductStats.Integrations.Coinbase;
using ProductStats.Mappers;
using ProductStats.Models;

namespace ProductStats.Services
{
    public class ProductTicker
    {
        private Rate _lastRate = new Rate(); // Leave it blank for now.
        private readonly Queue<Rate> _rates = new Queue<Rate>();
        private const int NumberOfRatesToKeep = 300;
        private readonly string _productId;
        private int _tickCount = 0;

        public event EventHandler<Rate> OnTick;

        public ProductTicker(Rate initialRate)
        {
            _productId = initialRate.ProductId;
            this.Tick(initialRate);
        }

        public int TickCount => _tickCount;

        public string ProductId => _productId;

        public Rate Last => _lastRate;

        /// <summary>
        /// Sets the last rate to the specified value, and raises the Tick Event.
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        internal Rate Tick(Rate rate)
        {
            Debug.Assert(rate.ProductId == this.ProductId);

            lock (_rates)
            {
                _rates.Enqueue(rate);
                while (_rates.Count > NumberOfRatesToKeep)
                {
                    _rates.Dequeue();
                }
            }

            lock (_lastRate)
            {
                _lastRate = rate;
            }

            _tickCount++;
            OnTick?.Invoke(this, _lastRate);

            return _lastRate;
        }
    }
}
