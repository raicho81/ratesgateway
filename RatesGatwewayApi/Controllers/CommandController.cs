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
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RatesGatwewayApi.Controllers
{
    [Route("xml_api/[controller]")]
    [ApiController]
    public class CommandController : BaseApiController
    {
        private readonly int expireTimeSeconds = 30;
        public CommandController(ExchangeRatesContext db,
                                 ILogger<CurrentController> logger,
                                 IConfiguration conf,
                                 IHttpClientFactory clientFactory,
                                 IConnectionMultiplexer redisMuxer) :
            base(db, logger, conf, clientFactory, redisMuxer)
        {
        }

        // POST api/<CommandController>
        [HttpPost]
        [Produces("application/xml")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<XMLCommandResponse>> Post([FromBody] XMLCommand xmlCmd)
        {
            Guid requestUUID;
            try
            {
                requestUUID = Guid.Parse(xmlCmd.id);
            }
            catch (FormatException e)
            {
                var errorResponse = new XMLCommandResponse
                {
                    statusCode = (int)ResponseStatusCodes.InvalidUUID,
                    statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.InvalidUUID],
                };
                return BadRequest(errorResponse);
            }

            // Check in Redis if the request was served already and add it to the set of served requests if not already present 
            IDatabase redisConn = _redisMuxer.GetDatabase();
            if (!await redisConn.SetAddAsync(servedRequestsIDsSetKey, xmlCmd.id))
            {
                var errorResponse = new XMLCommandResponse
                {
                    statusCode = (int)ResponseStatusCodes.RequestExists,
                    statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.RequestExists],
                };
                return BadRequest(errorResponse);
            }

            if (xmlCmd.cmdGet != null)
            {

                // Check for cached rate value in Redis, if there is no value in Redis the Rates Collector didn't pull any data yet
                string redisRateKey = $"current:{baseCurrency}:{xmlCmd.cmdGet.currency}";
                string timestampAndValueStr = await redisConn.StringGetAsync(redisRateKey);
                if (timestampAndValueStr == RedisValue.Null)
                {
                    var errorResponse = new XMLCommandResponse
                    {
                        statusCode = (int)ResponseStatusCodes.NoData,
                        statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.NoData],
                    };
                    return Created("PostCommandGet", errorResponse);
                }

                string[] timeStampAndRate = timestampAndValueStr.Split(":");
                double currTimestamp = Convert.ToDouble(timeStampAndRate[0]);
                double currRate = Convert.ToDouble(timeStampAndRate[1]);

                // Send stats
                var stats = new StatsRequest
                {
                    RequestId = xmlCmd.id,
                    ClientId = xmlCmd.cmdGet.consumer,
                    ServiceName = "EXT_Service_1",
                    Timestamp = (int)TimestampConversions.DateTimeToUnixTime(DateTime.Now.ToUniversalTime())
                };
                await SendStats(stats);

                var response = new XMLCommandResponse
                {
                    timestamp = currTimestamp,
                    statusCode = (int)ResponseStatusCodes.Success,
                    statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success],
                    rate = currRate.ToString(),
                    currency = xmlCmd.cmdGet.currency
                };

                return Created("PostCommandGet", response);
            }

            if (xmlCmd.cmdHist != null)
            {
                if (xmlCmd.cmdHist.period <= 0)
                {
                    var errorResponse = new XMLCommandResponse
                    {
                        statusCode = (int)ResponseStatusCodes.InvalidPeriod,
                        statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.InvalidPeriod],
                    };
                    return BadRequest(errorResponse);
                }

                // Send stats
                double timestampNow = (int)TimestampConversions.DateTimeToUnixTime(DateTime.Now.ToUniversalTime());
                var stats = new StatsRequest
                {
                    RequestId = xmlCmd.id,
                    ClientId = xmlCmd.cmdHist.consumer,
                    ServiceName = "EXT_Service_1",
                    Timestamp = timestampNow
                };
                await SendStats(stats);

                string redisHistHashKey = $"history:{baseCurrency}:{xmlCmd.cmdHist.currency}:{xmlCmd.cmdHist.period}";
                string historyData = await redisConn.StringGetAsync(redisHistHashKey);
                if (historyData != RedisValue.Null)
                {
                    DateTime dtNow = DateTime.Now.ToUniversalTime();
                    var response = new XMLCommandResponse()
                    {
                        timestamp = timestampNow,
                        currency = xmlCmd.cmdHist.currency,
                        statusCode = (int)ResponseStatusCodes.Success,
                        statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]
                    };

                    string[] historyDataEntries = historyData.Split(";");
                    response.timestampRatePairs = new List<TimestampRatePair>();
                    foreach (var entry in historyDataEntries)
                    {
                        string[] entryKeyVal = entry.Split(":");
                        response.timestampRatePairs.Add(new TimestampRatePair { timestamp = $"{entryKeyVal[0]}", rate = double.Parse(entryKeyVal[1])});
                    }
                    return Created("PostCommandHistory", response);
                }
                {
                    DateTime dtNow = DateTime.Now.ToUniversalTime();
                    DateTime dtBegin = dtNow.AddMilliseconds(-xmlCmd.cmdHist.period * 3600 * 1000);
                    var erates = db.ExchangeRates
                        .Where(er => (er.Timestamp <= dtNow) && (er.Timestamp >= dtBegin))
                        .Include(er => er.Rates)
                        .ToList();

                    if (!erates.Any())
                    {
                        var errorResponse = new XMLCommandResponse
                        {
                            statusCode = (int)ResponseStatusCodes.NoDataForPeriod,
                            statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.NoDataForPeriod],
                        };
                        return Created("PostCommandHistory", errorResponse);
                    }

                    var response = new XMLCommandResponse()
                    {
                        timestamp = timestampNow,
                        currency = xmlCmd.cmdHist.currency,
                        statusCode = (int)ResponseStatusCodes.Success,
                        statusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]
                    };

                    StringBuilder histData = new StringBuilder();
                    response.timestampRatePairs = new List<TimestampRatePair>();
                    foreach (var erate in erates)
                    {
                        var rate = erate.Rates.Where(r => r.Symbol == xmlCmd.cmdHist.currency).First();
                        double timestamp = TimestampConversions.DateTimeToUnixTime(erate.Timestamp);
                        histData.Append($"{timestamp}:{rate.RateValue};");
                        response.timestampRatePairs.Add(new TimestampRatePair { timestamp = $"{timestamp}", rate = rate.RateValue });
                    }
                    histData.Remove(histData.Length - 1, 1);

                    // Saved history data in Redis will expire after 60 seconds
                    redisConn.StringSet(redisHistHashKey, histData.ToString(), new TimeSpan(0, 0, expireTimeSeconds));

                    return Created("PostCommandHistory", response);
                }
            }

            return BadRequest();
        }

    }
}
