using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Timers;
using System.Linq;
using RatesCollector.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace RatesCollector
{
    class Collector
    {
        private readonly HttpClient client = new HttpClient(
            new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }
        );
        private System.Timers.Timer timer;
        private string apiUrl;
        private string apiKey;
        private string baseSymbol;
        private int requestRatesInterval;
        public Collector()
        {
            apiUrl = "http://data.fixer.io/api/latest";
            apiKey = "d795e3d09e9f5b98bbb4dba3192cdff4";
            baseSymbol = "EUR";
            requestRatesInterval = 3600; // Request exchange rates every hour
        }
        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var ratesData = await RequestRates();

            if (ratesData.Success)
                SaveRatesToDB(ratesData);
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach(var key in ratesData.Error.Keys)
                {
                    sb.Append($"{key}: {ratesData.Error[key]} ");
                }
                Logger.Log($"An error occured while requesting the exchange rates {sb.ToString()}");
            }
        }

        private void SetTimer()
        {
            timer = new System.Timers.Timer(requestRatesInterval*1000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void InitReqHeaders()
        {

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            client.DefaultRequestHeaders.Add("User-Agent", "Rates-Gateway-Rates-Collector");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            Logger.Log("- Exchange rates API Request Headers -");
            foreach (var header in client.DefaultRequestHeaders)
            {
                Logger.Log($"{header.Key}:");
                foreach (string val in header.Value)
                {
                    Logger.Log(val);
                }
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private void SaveRatesToDB(RatesData ratesData)
        {
            using (var db = new ExchangeRatesContext())
            {
                try
                {
                    // Check if exchange rates record with the same timestamp already exists in the DB
                    Logger.Log($"Querying DB for ExchangeRates table record with base {ratesData.Base} and timestamp {ratesData.Timestamp} ({UnixTimeStampToDateTime(ratesData.Timestamp).ToString()})");
                    int count = db.ExchangeRates
                        .Count(er => (er.Timestamp == UnixTimeStampToDateTime(ratesData.Timestamp)) &&
                                     (er.Base == ratesData.Base));
                    if (count != 0 )
                    {
                        Logger.Log($"ExchangeRates table record for base {ratesData.Base} and timestamp {ratesData.Timestamp} ({UnixTimeStampToDateTime(ratesData.Timestamp).ToString()}) already exists in DB. Skipping DB update.");
                        return;
                    }
                    
                    // Create
                    Logger.Log($"Inserting new exchange rates in DB for base {ratesData.Base} with timestamp {ratesData.Timestamp} ({UnixTimeStampToDateTime(ratesData.Timestamp).ToString()})");
                    db.Add(new ExchangeRates { Base = ratesData.Base, Timestamp = UnixTimeStampToDateTime(ratesData.Timestamp) });
                    db.SaveChanges();

                    // Read the last inserted record from ExchangeRates
                    Logger.Log("Querying DB for the last inserted exchange rates record id");
                    var er = db.ExchangeRates
                        .OrderByDescending(er => er.ExchangeRatesId)
                        .First();

                    // Update
                    Logger.Log($"Updating the rates. Adding new rates in DB for base {ratesData.Base} with timestamp {ratesData.Timestamp} ({UnixTimeStampToDateTime(ratesData.Timestamp).ToString()})");
                    foreach (var symbol in ratesData.Rates.Keys)
                    {
                        er.Rates.Add(
                            new Rate
                            {
                                Symbol = symbol.ToString(),
                                RateValue = Convert.ToDouble(ratesData.Rates[symbol].ToString())
                            });
                    }
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException e)
                {
                    Logger.Log(e.ToString());
                }
                catch (DbUpdateException e)
                {
                    Logger.Log(e.ToString());
                }
                catch (ArgumentNullException e)
                {
                    Logger.Log(e.ToString());
                }
                catch (OverflowException e)
                {
                    Logger.Log(e.ToString());
                }
                catch (InvalidOperationException e)
                {
                    Logger.Log(e.ToString());
                }
            }
        }

        private async Task<RatesData> RequestRates()
        {
            Task<System.IO.Stream> ratesStreamTask;
            RatesData ratesData = new RatesData();

            try
            {
                Logger.Log($"Requesting exchange rates for base {baseSymbol} from {apiUrl}");
                ratesStreamTask = client.GetStreamAsync($"{apiUrl}?access_key={apiKey}&base={baseSymbol}");
                ratesData = await JsonSerializer.DeserializeAsync<RatesData>(await ratesStreamTask);
                Logger.Log($"Exchange rates for base {baseSymbol} with timestamp {ratesData.Timestamp} ({UnixTimeStampToDateTime(ratesData.Timestamp).ToString()}) received from {apiUrl}");
            }
            catch (ArgumentNullException  e)
            {
                Logger.Log(e.ToString());
            }
            catch (HttpRequestException e)
            {
                Logger.Log(e.ToString());
            }
            catch (JsonException e)
            {
                Logger.Log(e.ToString());
            }

            return ratesData;
        }

        public void Start()
        {
            Logger.Log("***   Rates collector started   ***");
            Logger.Log($"Exchange rates API URL: {apiUrl}");
            Logger.Log($"Base: {baseSymbol}");
            Logger.Log($"Request rates interval: {requestRatesInterval}");
            InitReqHeaders();
            SetTimer();
        }
    }
}
