using System;
using System.Text.Json.Serialization;


namespace RatesGatwewayApi.Models
{
    public class ErrorResponse
    {
        public int Status { get; set; }
        public string StatusMessage { get; set; }
    }
}
