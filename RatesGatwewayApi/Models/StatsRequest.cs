using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RatesGatwewayApi.Models
{
    public class StatsRequest
    {
        public string ServiceName { get; set; }
        public string RequestId { get; set; }
        public double Timestamp { get; set; }
        public string ClientId { get; set; }
    }
}
