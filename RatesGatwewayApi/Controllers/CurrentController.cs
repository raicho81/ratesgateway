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
using Microsoft.Extensions.Caching.Distributed;
using RatesGatwewayApi.Models;
using StackExchange.Redis;


namespace RatesGatwewayApi.Controllers
{
    [Route("json_api/[controller]")]
    [ApiController]
    public class CurrentController : BaseApiController
    {
        public CurrentController(ExchangeRatesContext db,
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
        public async Task<ActionResult<CurrentResponse>> Post([FromBody] CurrentRequest value)
        {
            _logger.LogDebug($"Processing incoming request. ClientId:{value.ClientId}, RequestId:{value.RequestId}, Currency:{value.Currency}, Timestamp:{value.Timestamp}");
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
            if (!redisConn.SetAdd("serverd:req:ids", value.RequestId))
            {
                var errorResponse = new ErrorResponse
                {
                    Status = (int)ResponseStatusCodes.RequestExists,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.RequestExists],
                };
                return BadRequest(errorResponse);
            }

            // Check for cached value in Redis, if there is no value in Redis the Rates Collector didn't pull any data yet
            string redisRateKey = $"current:{baseCurrency}:{value.Currency}";
            string timestampAndValueStr = await redisConn.StringGetAsync(redisRateKey);
            if (timestampAndValueStr == RedisValue.Null)
            {
                var errorResponse = new CurrentResponse
                {
                    Status = (int)ResponseStatusCodes.NoData,
                    StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.NoData],
                };
                return Created("PostCurrent", errorResponse);
            }

            string[] timeStampAndRate = timestampAndValueStr.Split(":");
            double currTimestamp = Convert.ToDouble(timeStampAndRate[0]);
            double currRate = Convert.ToDouble(timeStampAndRate[1]);

            //if (!db.ExchangeRates.Any())
            //{
            //    var errorResponse = new CurrentResponse
            //    {
            //        Status = (int) ResponseStatusCodes.NoData,
            //        StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.NoData],
            //    };
            //    return Created("PostCurrent", errorResponse);
            //}

            //var erates = db.ExchangeRates
            //    .OrderByDescending(er => er.Timestamp)
            //    .First();

            //var rate = db.Rates
            //    .Where(s => s.ExchangeRatesId == erates.ExchangeRatesId && s.Symbol == value.Currency)
            //    .First();

            // Send stats
            var stats = new StatsRequest
            {
                RequestId = value.RequestId,
                ClientId = value.ClientId,
                ServiceName = "EXT_Service_2",
                Timestamp = value.Timestamp
            };
            await SendStats(stats);

            var response = new CurrentResponse
            {
                Timestamp = TimestampConversions.UnixTimeStampToDateTime(currTimestamp),
                Base = baseCurrency,
                Status = (int) ResponseStatusCodes.Success,
                StatusMessage = ResponseStatusMessages.Messages[(int) ResponseStatusCodes.Success],
                ExchangeRate = currRate,
                Symbol = value.Currency
            };

            return Created("PostCurrent", response);
        }
    }
}
