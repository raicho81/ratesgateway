using System;
using System.Text.Json.Serialization;
using System.Collections;

namespace RatesCollector
{
    class RatesData
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("timestamp")]

        public double Timestamp { get; set; }

        [JsonPropertyName("base")]
        public string Base { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("rates")]
        public Hashtable Rates { get; set; }
        [JsonPropertyName("error")]
        public Hashtable Error { get; set; } 
    }
}
