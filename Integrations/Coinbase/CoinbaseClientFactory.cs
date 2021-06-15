using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductStats.Integrations.Coinbase
{
    public class CoinbaseClientFactory
    {
        public ResillientHttpClient GetClient(CoinbaseConnection connection)
        {
            return new ResillientHttpClient(connection);
        }
    }
}
