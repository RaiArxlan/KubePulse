using Microsoft.EntityFrameworkCore;
using ProcessorApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ProcessorApi.Services
{
    public class ProcessConsumerService : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly IConfiguration _config;
        private readonly IDbContextFactory<RequestDbContext> _dbContextFactory;
        private readonly string _queue;
        private readonly ILogger<ProcessConsumerService> _logger;
        private readonly string _dlxName = "dlx";

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

            _queue = _config["RabbitMq:QueueName"] ?? throw new ArgumentNullException("RabbitMq:QueueName is not configured.");
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // Declare DLX
            await _channel.ExchangeDeclareAsync(_dlxName, ExchangeType.Fanout, durable: true);

            // Declare queue with DLX binding
            await _channel.QueueDeclareAsync(
                queue: _queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _dlxName }
                },
                cancellationToken: cancellationToken
            );

            // Limit unacked messages per consumer
            await _channel.BasicQosAsync(0, 1, false, cancellationToken);

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
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        return;
                    }

                    requestLog = JsonSerializer.Deserialize<RequestLog>(message);
                    if (requestLog == null)
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        return;
                    }

                    // Simulate some work
                    var random = new Random().Next(1, 5);
                    Task.Delay(random * 1000, stoppingToken).Wait(stoppingToken);

                    using var db = _dbContextFactory.CreateDbContext();
                    requestLog.EndTime = DateTime.UtcNow;

                    // Idempotent insert
                    bool exists = await db.RequestLogs.AnyAsync(x => x.Id == requestLog.Id, stoppingToken);
                    if (!exists)
                    {
                        db.RequestLogs.Add(requestLog);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("✅ Processed message with ID: {Id}", requestLog.Id);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Duplicate message skipped: {Id}", requestLog.Id);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing message ID: {Id}", requestLog?.Id);
                    // Send to DLQ, don't requeue
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                _queue,
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
}
