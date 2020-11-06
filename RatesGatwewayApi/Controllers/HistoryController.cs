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
using StackExchange.Redis;
using RatesGatwewayApi.Models;

namespace RatesGatwewayApi.Controllers
{
    [Route("json_api/[controller]")]
    [ApiController]
    public class HistoryController : BaseApiController
    {
        private readonly int expireTimeSeconds = 30;
        public HistoryController(ExchangeRatesContext db,
                                 ILogger<CurrentController> logger,
                                 IConfiguration conf,
                                 IHttpClientFactory clientFactory,
                                 IConnectionMultiplexer redisMuxer):
            base(db, logger, conf, clientFactory, redisMuxer)
        {
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

            // Check in Redis if the request was served already and add it to the set of served requests if not already present
            IDatabase redisConn = _redisMuxer.GetDatabase();
            if (!await redisConn.SetAddAsync(servedRequestsIDsSetKey, value.RequestId))
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
                ServiceName = "EXT_Service_2",
                Timestamp = value.Timestamp
            };
            await SendStats(stats);

            string redisHistHashKey = $"history:{baseCurrency}:{value.Currency}:{value.Period}";
            string historyData = await redisConn.StringGetAsync(redisHistHashKey);
            if(historyData != RedisValue.Null)
            {
                DateTime dtNow = DateTime.Now.ToUniversalTime();
                var response = new HistoryResponse()
                {
                    Timestamp = (int)TimestampConversions.DateTimeToUnixTime(dtNow.ToUniversalTime()),
                    Symbol = value.Currency,
                    Status = (int)ResponseStatusCodes.Success,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]
                };

                string[] historyDataEntries = historyData.Split(";");
                foreach (var entry in historyDataEntries)
                {
                    string[] entryKeyVal = entry.Split(":");
                    response.ExchangeRates.Add($"{entryKeyVal[0]}", double.Parse(entryKeyVal[1]));
                }
                return Created("PostHistory", response);
            }
            {
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
                    Timestamp = (int)TimestampConversions.DateTimeToUnixTime(dtNow.ToUniversalTime()),
                    Symbol = value.Currency,
                    Status = (int)ResponseStatusCodes.Success,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]
                };

                StringBuilder histData = new StringBuilder();
                foreach (var erate in erates)
                {
                    var rate = erate.Rates.Where(r => r.Symbol == value.Currency).First();
                    double timestamp = TimestampConversions.DateTimeToUnixTime(erate.Timestamp);
                    histData.Append($"{timestamp}:{rate.RateValue};");
                    response.ExchangeRates.Add($"{timestamp}", rate.RateValue);
                }
                histData.Remove(histData.Length - 1, 1);

                // Saved history data in Redis will expire after 60 seconds
                redisConn.StringSet(redisHistHashKey, histData.ToString(), new TimeSpan(0, 0, expireTimeSeconds));
                
                return Created("PostHistory", response);
            }
        }
    }
}
