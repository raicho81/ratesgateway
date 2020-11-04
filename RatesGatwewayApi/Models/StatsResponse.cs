using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RatesGatwewayApi.Models
{
    public class StatsResponse
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }
        [JsonProperty("statusMessage")]
        public string StatusMessage { get; set; }
    }
}
