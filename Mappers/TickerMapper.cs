using CbPT = CoinbasePro.Services.Products.Types;
using System;
using System.Collections.Generic;
using System.Text;
using ProductStats.Models;
using CoinbasePro.WebSocket.Models.Response;

namespace ProductStats.Mappers
{
    /// <summary>
    /// 
    /// </summary>
    /// <see cref="https://www.investopedia.com/terms/c/currencypair.asp"/>
    public static class TickerMapper
    {
        public static Models.Rate Map(this CbPT.ProductTicker ticker, string productId)
        {
            return new Models.Rate()
            {
                Timestamp = ticker.Time,
                ProductId = productId,
                Ask = ticker.Ask,
                Bid = ticker.Bid
            };
        }

        public static Models.Rate Map(this WebfeedEventArgs<Ticker> ticker)
        {
            return new Models.Rate()
            {
                Timestamp = ticker.LastOrder.Time.DateTime,
                ProductId = ticker.LastOrder.ProductId,
                Ask = ticker.LastOrder.BestAsk,
                Bid = ticker.LastOrder.BestBid
            };
        }
    }
}
