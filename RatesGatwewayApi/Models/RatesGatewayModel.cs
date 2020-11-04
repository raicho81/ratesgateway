using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RatesGatwewayApi.Models
{
    public class ExchangeRatesContext : DbContext
    {
        public DbSet<ExchangeRates> ExchangeRates { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<Stats> Stats { get; set; }
        public ExchangeRatesContext(DbContextOptions<ExchangeRatesContext> options)
            : base(options)
        {
        }
    }

    public class ExchangeRates
    {
        public int ExchangeRatesId { get; set; }
        //[Timestamp]
        public DateTime Timestamp { get; set; }
        public string Base { get; set; }
        public List<Rate> Rates { get; } = new List<Rate>();
    }

    public class Rate
    {
        public int RateId { get; set; }
        public int ExchangeRatesId { get; set; }
        public string Symbol { set; get; }
        public double RateValue { get; set; }
    }
    
    public class Stats
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public Guid RequestId { get; set; }
        public string ClientId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
