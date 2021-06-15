using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
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
    public class TickerManager
    {
        private readonly List<ProductTicker> _productTickers;
        private readonly CoinbaseConnectionFactory _connectionFactory;
        private readonly CoinbaseClientFactory _clientFactory;
        private object listLock = new object();

        public event EventHandler<EventArgs> OnAdded;

        public TickerManager(CoinbaseConnectionFactory connectionFactory,
                             CoinbaseClientFactory clientFactory,
                             ILoggerFactory loggerFactory)
        {
            _connectionFactory = connectionFactory;
            _clientFactory = clientFactory;
            _productTickers = new List<ProductTicker>();
        }

        public List<ProductTicker> ProductTickers => _productTickers;

        public ProductTicker this[string productId]
        {
            get
            {
                //_logger.LogInformation($"Getting ProductTicker for {productId}.");
                var productTicker = _productTickers.FirstOrDefault(pt => pt.ProductId == productId);
                if (productTicker == null)
                {
                    using (var connection = _connectionFactory.GetConnection())
                    using (var client = _clientFactory.GetClient(connection))
                    {
                        // Get the initial rate, so that the ProductTicker is initialised when created
                        // and doesn't have to wait for Chasnnel Manager to send it a tick.
                        var cbProductTicker = client.TryGetProductTicker(productId).Result;
                        var initialRate = TickerMapper.Map(cbProductTicker, productId);
                        productTicker = new ProductTicker(initialRate);
                        lock (listLock)
                        {
                            _productTickers.Add(productTicker);
                        }
                        OnAdded?.Invoke(this, new EventArgs());
                    }
                }
                return productTicker;
            }
        }
    }
}
