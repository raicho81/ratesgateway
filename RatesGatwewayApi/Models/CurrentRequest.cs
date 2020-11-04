using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace RatesGatwewayApi.Models
{
    public class CurrentRequest
    {
        [JsonPropertyName("requestId")]
        [Required]
        public string RequestId { set; get; }

        [Required]
        [JsonPropertyName("timestamp")]
        public double Timestamp { set; get; }

        [Required]
        [JsonPropertyName("client")]
        public string ClientId { set; get; }

        [Required]
        [JsonPropertyName("currency")]
        public string Currency { set; get; }
    }
}
