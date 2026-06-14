using System.Text;
using AuditService.Data;
using AuditService.Entities;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuditService.Messaging;

public sealed class RabbitMqEventConsumer(
    IOptions<RabbitMqSettings> settings,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = settings.Value;
        var factory = new ConnectionFactory { HostName = config.Host };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare(config.Exchange, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(config.Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(config.Queue, config.Exchange, "#");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var routingKey = args.RoutingKey;

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = "system",
                Action = routingKey,
                EntityType = routingKey.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "event",
                Payload = payload
            };

            dbContext.AuditLogs.Add(log);
            await dbContext.SaveChangesAsync(stoppingToken);

            channel.BasicAck(args.DeliveryTag, false);
        };

        channel.BasicConsume(config.Queue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }
}
