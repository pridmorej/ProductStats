using CoinbasePro.Network.Authentication;
using CoinbasePro.Network.HttpClient;
using CoinbasePro.WebSocket;
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
    public class CoinbaseConnection : IDisposable
    {
        public bool IsSandbox { get; internal set; }

        public bool IsAuthenticated { get; internal set; }

        public HttpClient HttpClient { get; internal set; }

        public IWebSocket WebSocket { get; internal set; }

        public CoinbasePro.CoinbaseProClient Client { get; internal set; }

        public void Dispose()
        {
            if (WebSocket != null)
            {
                WebSocket.Stop();
                WebSocket = null;
            }
            Client = null;
            HttpClient = null;
        }
    }
}
