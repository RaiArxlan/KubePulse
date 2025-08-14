using Microsoft.AspNetCore.Connections;
using ProcessorApi.Interface;
using RabbitMQ.Client;
using System.Text;

namespace ProcessorApi.Services;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

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
        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);

        _logger.LogInformation("Message published to queue {QueueName}: {Message}", queueName, message);
    }
}
