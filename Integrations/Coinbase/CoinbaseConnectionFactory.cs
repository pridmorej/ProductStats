using CoinbasePro.Network.Authentication;
using CoinbasePro.Network.HttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ProductStats.Models;
using ProductStats.Services;

namespace ProductStats.Integrations.Coinbase
{
    public class CoinbaseConnectionFactory
    {
        /// <summary>
        /// Creates an UnAuthenticated Connection to Coinbase.
        /// </summary>
        /// <returns></returns>
        public CoinbaseConnection GetConnection()
        {
            var client = new CoinbasePro.CoinbaseProClient(false);

            var connection = new CoinbaseConnection()
            {
                Client = client,
                HttpClient = null,
                IsAuthenticated = false,
                IsSandbox = false,
                WebSocket = client.WebSocket
            };

            return connection;
        }
    }
}
