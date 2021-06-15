using CoinbasePro;
using CoinbasePro.Exceptions;
using CbPT = CoinbasePro.Services.Products.Types;
using ProductStats.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProductStats.Integrations.Coinbase
{
    /// <summary>
    /// Provides a resillient interface to all the CoinbaseProClient Services.
    /// </summary>
    /// <remarks>
    /// This utilises a CoinbaseConnection that has been connected using a specific ApiKey for use with a single Portfolio.
    /// A separate ResilientHttpClient is required to interact with each Portfolio.
    /// </remarks>
    public class ResillientHttpClient : IDisposable
    {
        #region private members
        private readonly CoinbaseConnection _connection;
        private const int MaxRateLimitAttempts = 5;
        #endregion

        public ResillientHttpClient(CoinbaseConnection coinbaseConnection)
        {
            _connection = coinbaseConnection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        #region Products

        /// <summary>
        /// Get information about the last trade(tick), best bid/ ask and 24h volume.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<CbPT.ProductTicker> TryGetProductTicker(string productId)
        {
            var attempts = 0;
            while (attempts < MaxRateLimitAttempts)
            {
                try
                {
                    return await _connection.Client.ProductsService.GetProductTickerAsync(productId);
                }
                catch (CoinbaseProHttpException ex)
                {
                    if (ex.Message.IndexOf("Rate Limit Exceeded", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        Thread.Sleep(300);
                        attempts++;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            // If got to here then must have exceeded maxAttempts.
            throw new Exception("Max Attempts exceeded trying to get Product Ticker.");
        }

        public async Task<CbPT.ProductStats> TryGetProductStatsAsync(string productId)
        {
            var attempts = 0;
            while (attempts < MaxRateLimitAttempts)
            {
                try
                {
                    var result = await _connection.Client.ProductsService.GetProductStatsAsync(productId);
                    return result;
                }
                catch (CoinbaseProHttpException ex)
                {
                    if (ex.Message.IndexOf("Rate Limit Exceeded", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        Thread.Sleep(300);
                        attempts++;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            // If got to here then must have exceeded maxAttempts.
            throw new Exception($"Max Attempts exceeded calling Coinbase.ProductsService.GetProductStatsAsync for {productId}.");
        }

        #endregion
    }
}
