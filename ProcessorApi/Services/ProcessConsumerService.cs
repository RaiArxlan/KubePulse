using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ProcessorApi.Services;

public class ProcessConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IConfiguration _config;
    private readonly IDbContextFactory<RequestDbContext> _dbContextFactory;
    private readonly string queue;
    private readonly ILogger<ProcessConsumerService> _logger;

    public ProcessConsumerService(IConfiguration config, IDbContextFactory<RequestDbContext> dbContextFactory, ILogger<ProcessConsumerService> logger)
    {
        _config = config;
        _dbContextFactory = dbContextFactory;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMq:Host"],
            Port = int.Parse(config["RabbitMq:Port"]),
            UserName = config["RabbitMq:Username"],
            Password = config["RabbitMq:Password"]
        };
        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;

        queue = _config["RabbitMq:QueueName"] ?? throw new ArgumentNullException("RabbitMq:QueueName is not configured.");
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            RequestLog? requestLog = null;
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (string.IsNullOrEmpty(message)) return;

                requestLog = JsonSerializer.Deserialize<RequestLog>(message);
                if (requestLog == null) return;

                using var db = _dbContextFactory.CreateDbContext();
                requestLog.EndTime = DateTime.UtcNow;
                db.RequestLogs.Add(requestLog);
                await db.SaveChangesAsync(stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                _logger.LogInformation($"✅ Processed message with ID: {requestLog.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error : {Details}, {Id}", ex.Message, requestLog!.Id);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.CloseAsync(cancellationToken);
        _connection?.CloseAsync(cancellationToken);
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}