using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RatesGatwewayApi.Models;

namespace RatesGatwewayApi.Controllers
{
    [Route("json_api/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
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
        public HistoryController(ExchangeRatesContext db, ILogger<CurrentController> logger, IConfiguration conf) // IHttpClientFactory clientFactory;
        {
            this.db = db;
            _logger = logger;
            this.Configuration = conf;
            //this._clientFactory = clientFactory;
        }
        // POST api/<ValuesController>
        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<HistoryResponse>> Post([FromBody] HistoryRequest value)
        {
            if (value.Period <=0)
            {
                var errorResponse = new ErrorResponse
                {
                    Status = (int)ResponseStatusCodes.InvalidPeriod,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.InvalidPeriod],
                };
                return BadRequest(errorResponse);
            }

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

            var reqExists = db.Stats.Where(s => s.RequestId == requestUUID).Count();
            if (reqExists != 0)
            {
                var errorResponse = new ErrorResponse
                {
                    Status = (int)ResponseStatusCodes.RequestExists,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.RequestExists],
                };
                return Created("PostHistory", errorResponse);
            }

            //db.Stats.Add(new Stat
            //{
            //    ClientId = value.ClientId,
            //    RequestId = requestUUID,
            //    ServiceName = "EXT_Service_1",
            //    Timestamp = TimestmpConversions.UnixTimeStampToDateTime(value.Timestamp)
            //});
            //await db.SaveChangesAsync();

            DateTime dtNow = DateTime.Now.ToUniversalTime();
            DateTime dtBegin = dtNow.AddMilliseconds(-value.Period * 3600 * 1000);
            var erates = db.ExchangeRates
                .Where(er => (er.Timestamp <= dtNow) && (er.Timestamp >= dtBegin))
                .Include(er => er.Rates)
                .ToList();
            
            if (!erates.Any())
            {
                var errorResponse = new ErrorResponse
                {
                    Status = (int)ResponseStatusCodes.NoDataForPeriod,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.NoDataForPeriod],
                };
                return Created("PostHistory", errorResponse);
            }

            var response = new HistoryResponse()
            {
                Timestamp = TimestampConversions.DateTimeToUnixTime(dtNow),
                Symbol = value.Symbol,
                Status = (int)ResponseStatusCodes.Success,
                StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]
            };

            foreach (var erate in erates)
            {
                var rate = erate.Rates.Where(r => r.Symbol == value.Symbol).First();
                response.ExchangeRates.Add($"{TimestampConversions.DateTimeToUnixTime(erate.Timestamp)}", rate.RateValue);
            }

            return Created("PostHistory", response);
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
