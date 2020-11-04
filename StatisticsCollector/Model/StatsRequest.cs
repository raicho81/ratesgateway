using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace StatisticsCollector.Model
{
    public class StatsRequest
    {
        [JsonPropertyName("serviceName")]
        [Required]
        public string ServiceName { set; get; }

        [JsonPropertyName("requestId")]
        [Required]
        public string RequestId { set; get; }

        [JsonPropertyName("timestamp")]
        [Required]
        public double Timestamp { set; get; }

        [JsonPropertyName("clientId")]
        [Required]
        public string ClientId { get; set; }
    }
}
