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
    public class HistoryController : BaseApiController
    {
        public HistoryController(ExchangeRatesContext db, ILogger<CurrentController> logger, IConfiguration conf, IHttpClientFactory clientFactory)
        {
            this.db = db;
            _logger = logger;
            Configuration = conf;
            _clientFactory = clientFactory;
        }

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
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.InvalidUUID],
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
                return BadRequest(errorResponse);
            }

            // Send stats
            var stats = new StatsRequest
            {
                RequestId = value.RequestId,
                ClientId = value.ClientId,
                ServiceName = "EXT_Service_1",
                Timestamp = value.Timestamp
            };
            await SendStats(stats);

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
                Timestamp = (int)TimestampConversions.DateTimeToUnixTime(dtNow),
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
    }
}
