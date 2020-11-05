using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RatesGatwewayApi;
using StatisticsCollector.Model;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace StatisticsCollector.Controllers
{
    [Route("json_api/[controller]")]
    [ApiController]
    public class AddController : ControllerBase
    {
        private readonly ILogger _logger;

        private StatsContext db;
        public AddController(StatsContext db, ILogger<AddController> logger)
        {
            this.db = db;
            this._logger = logger;
        }

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatsResponse>> Post([FromBody] StatsRequest value)
        {
            Guid requestUUID;
            try
            {
                requestUUID = Guid.Parse(value.RequestId);
            }
            catch (FormatException)
            {
                _logger.LogError($"Inavlid uuid format: {value.RequestId}");
                return BadRequest(new StatsResponse
                    {
                        StatusCode = (int)ResponseStatusCodes.InvalidRequestId,
                        StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.InvalidRequestId]
                    }
                );
            }

            _logger.LogInformation($"Saving stats to DB: RequestId:{value.RequestId}, ClientId:{value.ClientId}, ServiceName:{value.ServiceName},Timestamp:{value.Timestamp}");
            db.Stats.Add(new Stats
                {
                    RequestId = requestUUID,
                    ClientId = value.ClientId,
                    ServiceName = value.ServiceName,
                    Timestamp = TimestampConversions.UnixTimeStampToDateTime(value.Timestamp),
                }
            );
            try { 
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                _logger.LogError(e.ToString());
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e.ToString());
            }

            // TODO: Send to RabbitMQ

            return Created("PostAdd",
                        new StatsResponse{
                            StatusCode = (int)ResponseStatusCodes.Success,
                            StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]}
                        );
        }
    }
}
