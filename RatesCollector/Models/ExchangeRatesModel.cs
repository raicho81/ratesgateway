using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RatesCollector.Models
{
    public class ExchangeRatesContext : DbContext
    {
        public DbSet<ExchangeRates> ExchangeRates { get; set; }
        public DbSet<Rate> Rates { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql("Host=db;Database=postgres;Username=postgres;Password=secret");
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
}
