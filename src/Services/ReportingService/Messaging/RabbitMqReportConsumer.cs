using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportingService.Data;
using ReportingService.Entities;

namespace ReportingService.Messaging;

public sealed class RabbitMqReportConsumer(
    IOptions<RabbitMqSettings> settings,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqReportConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IConnection? connection;
    private IModel? channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = settings.Value;
        var factory = new ConnectionFactory { HostName = config.Host };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        channel.ExchangeDeclare(config.Exchange, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(config.Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(config.Queue, config.Exchange, "reports.projectStatsChanged");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            var message = Encoding.UTF8.GetString(args.Body.ToArray());

            try
            {
                var payload = JsonSerializer.Deserialize<ProjectReportStatsChangedPayload>(message, JsonOptions);
                if (payload is not null)
                {
                    await UpsertReportAsync(payload, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process project report update event.");
            }

            channel.BasicAck(args.DeliveryTag, false);
        };

        channel.BasicConsume(config.Queue, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        channel?.Dispose();
        connection?.Dispose();
        base.Dispose();
    }

    private async Task UpsertReportAsync(ProjectReportStatsChangedPayload payload, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var report = await dbContext.ProjectReports
            .FirstOrDefaultAsync(item => item.ProjectId == payload.ProjectId, cancellationToken);

        var progress = payload.TotalTasks == 0
            ? 0
            : Math.Round((double)payload.CompletedTasks / payload.TotalTasks * 100, 2);

        if (report is null)
        {
            report = new ProjectReport
            {
                Id = Guid.NewGuid(),
                ProjectId = payload.ProjectId
            };
            dbContext.ProjectReports.Add(report);
        }

        report.TotalTasks = payload.TotalTasks;
        report.CompletedTasks = payload.CompletedTasks;
        report.ProgressPercent = progress;
        report.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record ProjectReportStatsChangedPayload(
        Guid ProjectId,
        int TotalTasks,
        int CompletedTasks);
}
