using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
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
            if (xmlCmd.cmdGet != null)
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
                    return Created("PostCurrent", errorResponse);
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
                    rate = currRate,
                    currency = xmlCmd.cmdGet.currency
                };

                return Created("PostCurrent", response);
            }

            if (xmlCmd.cmdHist != null)
            {
                // history
            }

            return BadRequest();
        }

    }
}
