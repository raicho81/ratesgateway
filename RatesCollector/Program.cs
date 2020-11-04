using System;
using System.Threading.Tasks;
using System.Threading;

namespace RatesCollector
{
    class Program
    {
        private static readonly Collector collector = new Collector(); 
        static void Main(string[] args)
        {
            collector.Start();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
