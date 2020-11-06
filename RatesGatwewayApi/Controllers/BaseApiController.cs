using System;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using RatesGatwewayApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;


namespace RatesGatwewayApi.Controllers
{
    public class BaseApiController: ControllerBase
    {
        protected readonly string baseCurrency = "EUR";
        protected readonly string servedRequestsIDsSetKey = "served:req:ids";
        protected readonly ExchangeRatesContext db;
        protected readonly ILogger _logger;
        protected readonly IConfiguration Configuration;
        protected readonly IHttpClientFactory _clientFactory;
        protected readonly IConnectionMultiplexer _redisMuxer;

        public BaseApiController(ExchangeRatesContext db,
                                 ILogger<CurrentController> logger,
                                 IConfiguration conf,
                                 IHttpClientFactory clientFactory,
                                 IConnectionMultiplexer redisMuxer)
        {
            this.db = db;
            _logger = logger;
            Configuration = conf;
            _clientFactory = clientFactory;
            _redisMuxer = redisMuxer;
        }

        public async Task SendStats(StatsRequest stats)
        {
            var client = _clientFactory.CreateClient();
            // Set HTTP Headers
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            client.DefaultRequestHeaders.Add("User-Agent", "RatesGatewayApi");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            var statsResponse = new StatsResponse();
            _logger.LogDebug("Sending stats to stats collector service");
            try
            {
                string payload = JsonSerializer.Serialize(stats);
                HttpContent reqContent = new StringContent(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"http://{Configuration.GetValue<string>("StatsHost")}:{Configuration.GetValue<string>("StatsPort")}/json_api/add", reqContent);
                string respContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Stats collector response: {respContent}");
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e.ToString());
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e.ToString());
            }
            catch (JsonException e)
            {
                _logger.LogError(e.ToString());
            }
        }
    }
}
