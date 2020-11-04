using System;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RatesGatwewayApi.Models;

namespace RatesGatwewayApi.Controllers
{
    [Route("json_api/[controller]")]
    [ApiController]
    public class CurrentController : ControllerBase
    {
        private readonly ExchangeRatesContext db;
        private readonly HttpClient client = new HttpClient(
            new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }
        );
        private readonly ILogger _logger;
        public IConfiguration Configuration { get; }
        //private readonly IHttpClientFactory _clientFactory;
        public CurrentController(ExchangeRatesContext db, ILogger<CurrentController> logger, IConfiguration conf) // IHttpClientFactory clientFactory;
        {
            this.db = db;
            _logger = logger;
            this.Configuration = conf;
            //this._clientFactory = clientFactory;
        }

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<CurrentResponse>> Post([FromBody] CurrentRequest value)
        {
            _logger.LogInformation($"Processing incoming request. ClientId:{value.ClientId}, RequestId:{value.RequestId}, Currency:{value.Currency}, Timestamp:{value.Timestamp}");
            Guid requestUUID;
            try
            {
                requestUUID = Guid.Parse(value.RequestId);
            }
            catch (FormatException e)
            {
                var errorResponse = new ErrorResponse
                {
                    Status = (int)ResponseStatusCodes.InvalidUUID,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.InvalidPeriod],
                };
                return BadRequest(errorResponse);
            }

            // TODO: Redis chaching 

            var reqExists = db.Stats.Where(s => s.RequestId == requestUUID).Count();
            if (reqExists != 0)
            {
                var errorResponse = new ErrorResponse
                {
                    Status = (int) ResponseStatusCodes.RequestExists,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.RequestExists],
                };
                return BadRequest(errorResponse);
            }

            if (!db.ExchangeRates.Any())
            {
                var errorResponse = new CurrentResponse
                {
                    Status = (int) ResponseStatusCodes.NoData,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.NoData],
                };
                return Created("PostCurrent", errorResponse);
            }

            var erates = db.ExchangeRates
                .OrderByDescending(er => er.Timestamp)
                .First();

            var rate = db.Rates
                .Where(s => s.ExchangeRatesId == erates.ExchangeRatesId && s.Symbol == value.Currency)
                .First();

            // Send stats
            var stats = new StatsRequest
            {
                RequestId = value.RequestId,
                ClientId = value.ClientId,
                ServiceName = "EXT_Service_1",
                Timestamp = value.Timestamp
            };
            await SendStats(stats);

            var response = new CurrentResponse
            {
                Timestamp = erates.Timestamp,
                Base = erates.Base,
                Status = (int) ResponseStatusCodes.Success,
                StatusMessage = ResponseStatusMessages.Messages[(int) ResponseStatusCodes.Success],
                ExchangeRate = rate.RateValue,
                Symbol = rate.Symbol
            };

            return Created("PostCurrent", response);
        }
        public async Task SendStats(StatsRequest stats)
        {
            // Set HTTP Headers
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            client.DefaultRequestHeaders.Add("User-Agent", "RatesGatewayApi");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            var statsResponse = new StatsResponse();
            _logger.LogInformation("Sending stats to stats collector service");
            try
            {
                string payload = JsonSerializer.Serialize(stats);
                HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"http://{Configuration.GetValue<string>("StatsHost")}/json_api/add", content);
                statsResponse = await JsonSerializer.DeserializeAsync<StatsResponse>(await response.Content.ReadAsStreamAsync());
                _logger.LogInformation($"Stats service response: statusCode:{statsResponse.StatusCode}, statusMessage:{statsResponse.StatusMessage}");
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
