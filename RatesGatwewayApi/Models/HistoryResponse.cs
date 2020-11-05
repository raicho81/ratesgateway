using System;
using System.Text.Json.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace RatesGatwewayApi.Models
{
    public class HistoryResponse
    {
        public int Status { get; set; }

        public string StatusMessage { get; set; }

        public int Timestamp { get; set; }

        public string Symbol { get; set; }
        public Dictionary<string, double> ExchangeRates { get; set; } = new Dictionary<string, double>();
    }
}
