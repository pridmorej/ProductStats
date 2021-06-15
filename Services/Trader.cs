using CoinbasePro.Network.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProductStats.Integrations.Coinbase;
using ProductStats.Models;
using ProductStats.Services;

namespace ProductStats
{
    /// <summary>
    /// The orchestrator for all Trading Activities, i.e. Placing Orders, Managing Positions, Monitoring Feeds, etc.
    /// </summary>
    public class Trader : IHostedService, IDisposable
    {
        private readonly ChannelManager _channelManager;
        private readonly ProductStatsManager _productStatsManager;
        private Task _executingTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _finishedTrading = false;

        public void Dispose()
        {
            DetachEvents();
        }

        private void DetachEvents()
        {
        }

        #region IHostedService

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            #region When Cancelled
            cancellationToken.Register(() =>
            {
                while (!_finishedTrading)
                {
                    Thread.Sleep(50);
                }
                _channelManager.CloseChannels();
            });
            #endregion

            if (await PrepareForTrading())
            {
                await StartTrading(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                DetachEvents();

                // Signal cancellation to the executing method
                _cts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                                                              cancellationToken));
            }
        }

        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        public Trader(ChannelManager channelManager,
                      ProductStatsManager productStatsManager)
        {
            _channelManager = channelManager;
            _productStatsManager = productStatsManager;
        }

        /// <summary>
        /// Activities needed to complete before start trading.
        /// </summary>
        /// <returns></returns>
        private Task<bool> PrepareForTrading()
        {
            try
            {
                // TODO: Should also determine products based on any open Positions.
                var _markets = new List<string>() { "1INCH-EUR", "AAVE-EUR", "ADA-EUR", "ANKR-EUR", 
                                                    "BAND-EUR", "BAT-EUR", "BCH-EUR", "BTC-EUR", "CGLD-EUR", 
                                                    "CRV-EUR", "EOS-EUR", "ETC-EUR", "ETH-EUR", "FIL-EUR", 
                                                    "FORTH-EUR", "LINK-EUR", "LTC-EUR", "MATIC-EUR", "NMR-EUR", 
                                                    "NU-EUR", "OMG-EUR", "SKL-EUR", "SNX-EUR", "SUSHI-EUR", 
                                                    "XLM-EUR", "XTZ-EUR", "ZRX-EUR" };

                _channelManager.OpenChannels(_markets);

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// The main trading process.
        /// </summary>
        private async Task StartTrading(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Allow method to exit cleanly.
            }
            catch (Exception)
            {
                throw;
            }

            _finishedTrading = true;
        }
    }

}
