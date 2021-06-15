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
    /// <summary>
    /// Maintains a historical cache of 24hr ProductStats for a list of products.
    /// </summary>
    /// <remarks>
    /// Unlike the ProductTicker, there is no event on the GDAX Client that is raised when the 
    /// stats change, so retrieval needs to be managed here on a periodic basis.
    /// </remarks>
    public class ProductStatsUpdater : IDisposable
    {
        private readonly CoinbaseConnectionFactory _connectionFactory;
        private readonly CoinbaseClientFactory _clientFactory;
        private readonly ConcurrentDictionary<string, List<ProductStat>> _productStats = new ConcurrentDictionary<string, List<ProductStat>>();
        private const int NumberOfStatsToKeep = 15;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _executingTask;
        private SemaphoreSlim mutex = new SemaphoreSlim(1);
        private object listLock = new object();
        private readonly TimeSpan _schedule = new TimeSpan(0, 0, 30);
        private bool _finished = false;

        public event EventHandler<ProductStat> OnUpdated;

        public ProductStatsUpdater(CoinbaseConnectionFactory connectionFactory,
                                   CoinbaseClientFactory clientFactory)
        {
            _connectionFactory = connectionFactory;
            _clientFactory = clientFactory;
        }

        public void Dispose()
        {
            this.Stop();
            while (!_finished)
            {
                Thread.Sleep(50);
            }
            if (_productStats != null)
            {
                _productStats.Clear();
            }
        }

        /// <summary>
        /// Start monitoring the stats for the specified product, and return the latest.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<ProductStat> AddProduct(string productId)
        {
            var added = false;
            List<ProductStat> statsList = null;
            while (!added)
            {
                if (!_productStats.TryGetValue(productId, out statsList))
                {
                    // Not in dictionary, so add a new Stats list for the specified product.
                    var now = DateTime.UtcNow;
                    var currentTrigger = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind);
                    var stats = await this.GetProductStatsFromCoinbase(currentTrigger, productId);
                    statsList = new List<ProductStat>();
                    lock (listLock)
                    {
                        // Protect against "Item already in collection", as product may have been added to collection on a different thread since checking above.
                        added = _productStats.TryAdd(productId, statsList);
                        if (!added) continue;
                    }
                    lock (listLock)
                    {
                        statsList.Add(stats);
                    }
                    return stats;
                }
                else
                {
                    added = true;
                }
            }
            return await this.Last(productId);
        }

        /// <summary>
        /// Stop monitoring the stats for the specified product.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private void RemoveProduct(string productId)
        {
            if (_productStats.Any(kvp => kvp.Key == productId))
            {
                lock (listLock)
                {
                    List<ProductStat> statsList = null;
                    _productStats.Remove(productId, out statsList);
                }
            }
        }

        /// <summary>
        /// Return the latest stats for the specified product.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<ProductStat> Last(string productId)
        {
            List<ProductStat> statsList = null;
            var found = _productStats.TryGetValue(productId, out statsList);
            if (!found)
            {
                var ignore = await this.AddProduct(productId);
            }
            var stats = _productStats[productId].OrderByDescending(ps => ps.Date).FirstOrDefault();
            Debug.Assert(stats.ProductId == productId);
            return stats;
        }

        /// <summary>
        /// Return all the stats for the specified product in descending date order.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<List<ProductStat>> All(string productId)
        {
            List<ProductStat> statsList = null;
            var found = _productStats.TryGetValue(productId, out statsList);
            if (!found)
            {
                var ignore = await this.AddProduct(productId);
            }
            statsList = _productStats[productId].OrderByDescending(ps => ps.Date).ToList();
            Debug.Assert(statsList.All(ps => ps.ProductId == productId));
            return statsList;
        }

        #region Start, Execute and Stop methods.

        /// <summary>
        /// Start the ProductStatsUpdater which will then update the stats according to the configured schedule.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public Task Start()
        {
            _finished = false;

            // Store the task we're executing
            _executingTask = Execute(_cts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get the latest stats for each product we are monitoring.
        /// </summary>
        /// <remarks>
        /// This method is called by the Start method and continuously runs in a loop until cancelled.
        /// </remarks>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task Execute(CancellationToken cancellationToken)
        {
            #region When Cancelled
            cancellationToken.Register(() =>
            {
                // Wait until execution has finished.
                while (!_finished)
                {
                    Thread.Sleep(50);
                }
            });
            #endregion

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var currentTrigger = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind);

                    // Take a copy of all the products that are going to be updated to avoid CollectionModifiedException.
                    var productsToUpdate = _productStats.Select(ps => ps.Key).ToList();
                    foreach (var productId in productsToUpdate)
                    {
                        var stats = await this.GetProductStatsFromCoinbase(currentTrigger, productId);
                        lock (listLock)
                        {
                            _productStats[productId].Add(stats);
                            while (_productStats[productId].Count > NumberOfStatsToKeep)
                            {
                                _productStats[productId].RemoveAt(1);
                            }
                        }

                        OnUpdated?.Invoke(this, stats);
                    }

                    var nextTrigger = currentTrigger + _schedule;
                    //var cancelled = cancellationToken.WaitHandle.WaitOne(nextTrigger - DateTime.UtcNow);
                    var cancelled = cancellationToken.WaitHandle.WaitOne(_schedule);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            _finished = true;
        }

        /// <summary>
        /// Stops the execution of the ProductStatsUpdater.
        /// </summary>
        /// <returns></returns>
        public Task Stop()
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<ProductStat> GetProductStatsFromCoinbase(DateTime currentTrigger, string productId)
        {
            await mutex.WaitAsync();
            try
            {
                using (var connection = _connectionFactory.GetConnection())
                using (var client = _clientFactory.GetClient(connection))
                {
                    var productStats = await client.TryGetProductStatsAsync(productId).ConfigureAwait(false);
                    var dto = new ProductStat()
                    {
                        ProductId = productId,
                        Date = currentTrigger,
                        Open = productStats.Open,
                        Low = productStats.Low,
                        High = productStats.High,
                        Last = productStats.Last,
                        Volume = productStats.Volume,
                        Volume30Day = productStats.Volume30Day
                    };
                    return dto;
                }
            }
            finally
            {
                mutex.Release();
            }
        }
    }
}
