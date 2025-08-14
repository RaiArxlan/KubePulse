using ProcessorApi.Interface;
using RabbitMQ.Client;
using System.Text;

namespace ProcessorApi.Services
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly string _dlxName = "dlx";

        public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMq:Host"],
                Port = int.Parse(config["RabbitMq:Port"]),
                UserName = config["RabbitMq:Username"],
                Password = config["RabbitMq:Password"]
            };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _logger = logger;
        }

        public async Task Publish(string queueName, string message)
        {
            // Declare DLX (once)
            await _channel.ExchangeDeclareAsync(_dlxName, ExchangeType.Fanout, durable: true);

            // Declare queue with DLX binding
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _dlxName }
                });

            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);

            _logger.LogInformation("📤 Message published to queue {QueueName}: {Message}", queueName, message);
        }
    }
}
