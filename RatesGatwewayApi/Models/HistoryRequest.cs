using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace RatesGatwewayApi.Models
{
    public class HistoryRequest
    {
        [JsonPropertyName("requestId")]
        [Required]
        public string RequestId { get; set; }

        [JsonPropertyName("timestamp")]
        [Required]
        public double Timestamp { get; set; }

        [JsonPropertyName("client")]
        [Required]
        public string ClientId { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        public string Currency { get; set; }

        [JsonPropertyName("period")]
        [Required]
        public int Period { get; set; }
    }
}
