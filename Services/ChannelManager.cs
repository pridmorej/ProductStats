using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinbasePro;
using CoinbasePro.WebSocket;
using CoinbasePro.WebSocket.Models.Response;
using CoinbasePro.WebSocket.Types;
using Microsoft.Extensions.Logging;
using ProductStats.Integrations.Coinbase;
using ProductStats.Mappers;
using ProductStats.Models;
using WebSocket4Net;

namespace ProductStats.Services
{
    public class ChannelManager : IDisposable
    {
        private readonly TickerManager _tickerManager;
        private readonly IWebSocket WebSocket;
        private bool _opened = false;

        public void Dispose()
        {
            CloseChannels();
        }

        public ChannelManager(CoinbaseConnectionFactory coinbaseConnectionFactory,
                              TickerManager tickerManager)
        {
            _tickerManager = tickerManager;
            this.WebSocket = coinbaseConnectionFactory.GetConnection().WebSocket;
        }

        public void OpenChannels(List<string> markets)
        {
            if (_opened)
            {
                throw new Exception("Channel Manager already open");
            }

            var channels = new List<ChannelType>()
            {
                ChannelType.Heartbeat,
                ChannelType.Ticker
            };

            #region Attach Authenticated Event Handlers

            // ChannelType.Heartbeat
            this.WebSocket.OnHeartbeatReceived += AuthenticatedWebSocket_OnHeartbeatReceived;

            // ChannelType.Ticker
            this.WebSocket.OnTickerReceived += AuthenticatedWebSocket_OnTickerReceived;

            #endregion

            this.WebSocket.Start(markets, channels);

            _opened = true;
        }

        public void ChangeChannels(List<string> markets)
        {
            if (!_opened)
            {
                throw new Exception("Can't change channels when Channel Manager is not open");
            }

            // This current GDAX Library doesn't support just changing markets.  So need to stop and start the websocket.
            // TODO: have separate websockets.  One listening to tickers, the other listening to orders.
            this.CloseChannels();
            this.OpenChannels(markets);

        }

        public void CloseChannels()
        {
            if (this.WebSocket.State == WebSocketState.Open)
            {
                this.WebSocket.Stop();
            }

            // Wait for the last few messages to arrive.
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(200);
            }

            #region Detach Events
            this.WebSocket.OnHeartbeatReceived -= AuthenticatedWebSocket_OnHeartbeatReceived;
            this.WebSocket.OnTickerReceived -= AuthenticatedWebSocket_OnTickerReceived;
            #endregion

            _opened = false;
        }

        private void AuthenticatedWebSocket_OnTickerReceived(object sender, WebfeedEventArgs<Ticker> e)
        {
            // When a ticker is received, get the matching Ticker from the ticker manager.
            // This ensures that any Positions that use this ticker only get ticks for the product they're interested in.
            var productTicker = _tickerManager[e.LastOrder.ProductId];
            if (productTicker != null)
            {
                productTicker.Tick(TickerMapper.Map(e));
            }
        }

        private void AuthenticatedWebSocket_OnHeartbeatReceived(object sender, WebfeedEventArgs<Heartbeat> e)
        {
            // Do nothing
        }
    }
}
