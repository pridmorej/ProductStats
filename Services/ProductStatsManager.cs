using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProductStats.Integrations.Coinbase;
using ProductStats.Models;

namespace ProductStats.Services
{
    public class ProductStatsManager : IDisposable
    {
        private readonly ProductStatsUpdater _productStatsUpdater;

        public event EventHandler<ProductStat> OnUpdated;

        public ProductStatsManager(CoinbaseConnectionFactory connectionFactory,
                                   CoinbaseClientFactory clientFactory,
                                   ILoggerFactory loggerFactory)
        {
            _productStatsUpdater = new ProductStatsUpdater(connectionFactory, clientFactory);
            _productStatsUpdater.OnUpdated += _productStatsUpdater_OnUpdated;

            // Now start the background process running.
            // This will keep running for the lifetime of this ProductStatsManager.
            Task.Run(() => _productStatsUpdater.Start());
        }

        private void _productStatsUpdater_OnUpdated(object sender, ProductStat e)
        {
            // Bubble the event up.
            OnUpdated?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_productStatsUpdater != null)
            {
                _productStatsUpdater.Stop();
                _productStatsUpdater.OnUpdated -= _productStatsUpdater_OnUpdated;
            }
        }

        public async Task<ProductStat> LatestProductStatsFor(string productId)
        {
            var stats = await _productStatsUpdater.Last(productId);
            Debug.Assert(stats.ProductId == productId);
            return stats;
        }

        public async Task<List<ProductStat>> AllProductStatsFor(string productId)
        {
            var statsList = await _productStatsUpdater.All(productId);
            Debug.Assert(statsList.All(ps => ps.ProductId == productId));
            return statsList;
        }
    }
}
