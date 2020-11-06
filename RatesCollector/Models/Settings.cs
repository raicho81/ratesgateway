using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace RatesCollector.Models
{
    class Settings
    {
        [JsonPropertyName("apiUrl")]
        public string apiUrl { get; set; }
        
        [JsonPropertyName("apiKey")]
        public string apiKey { get; set; }
        
        [JsonPropertyName("baseCurrency")]
        public string baseCurrency { get; set; }

        [JsonPropertyName("requestRatesInterval")]
        public int requestRatesInterval { get; set; }

        [JsonPropertyName("redisHost")]
        public string redisHost { get; set; }

        [JsonPropertyName("redisPort")]
        public int redisPort { get; set; }

        [JsonPropertyName("redisPassword")]
        public string redisPassword { get; set; }

    }
}
