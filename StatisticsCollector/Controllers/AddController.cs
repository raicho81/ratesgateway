using System;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RatesGatwewayApi;
using StatisticsCollector.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;


namespace StatisticsCollector.Controllers
{
    [Route("json_api/[controller]")]
    [ApiController]
    public class AddController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly StatsContext db;
        private readonly IConfiguration Configuration;

        public AddController(StatsContext db, ILogger<AddController> logger, IConfiguration conf)
        {
            this.db = db;
            _logger = logger;
            Configuration = conf;
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

            PublishToRabbitMQ(value); // Send to RabbitMQ

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

            return Created("PostAdd",
                        new StatsResponse{
                            StatusCode = (int)ResponseStatusCodes.Success,
                            StatusMessage = ResponseStatusMessages.Messages[(int)ResponseStatusCodes.Success]}
                        );
        }

        private void PublishToRabbitMQ(StatsRequest statsRequest)
        {
            var factory = new ConnectionFactory() { HostName = Configuration.GetValue<string>("RabbitMQHostName") };
            try
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "StatsQueue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message = JsonSerializer.Serialize<StatsRequest>(statsRequest);
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: Configuration.GetValue<string>("RabbitMQExchange"),
                                         routingKey: "StatsQueue",
                                         basicProperties: null,
                                         body: body);
                    _logger.LogInformation("Sent {0} to RabbitMQ", message);
                }
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e.ToString());
            }
            catch (EncoderFallbackException e)
            {
                _logger.LogError(e.ToString());
            }
            catch (BrokerUnreachableException e)
            {
                _logger.LogError(e.ToString());
            }
        }
    }
}
