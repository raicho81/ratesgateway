using System;
using System.Text.Json.Serialization;

namespace RatesGatwewayApi.Models
{
    public class CurrentResponse
    {
        public int Status { get; set; }
        public string StatusMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public string Base { get; set; }
        public string Symbol { get; set; }
        public double ExchangeRate { get; set; }
    }
}
