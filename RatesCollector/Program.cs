using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using RatesCollector.Models;

namespace RatesCollector
{
    class Program
    {
        private static Collector collector;
        static void Main(string[] args)
        {
            Settings settings;
            try
            { 
                var jsonSettingsStr = File.ReadAllText("settings.json");
                settings = JsonConvert.DeserializeObject<Settings>(jsonSettingsStr);
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
                return;
            }
            collector = new Collector(settings.apiUrl, settings.apiKey, settings.baseCurrency, settings.requestRatesInterval, settings.redisHost, settings.redisPort, settings.redisPassword);
            collector.Start();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
