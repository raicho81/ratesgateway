using System;
using Microsoft.EntityFrameworkCore;

namespace StatisticsCollector.Model
{
    public class StatsContext : DbContext
    {
        public DbSet<Stats> Stats { get; set; }
        public StatsContext(DbContextOptions<StatsContext> options)
            : base(options)
        {
        }
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
