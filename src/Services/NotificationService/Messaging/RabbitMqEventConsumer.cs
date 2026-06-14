using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Messaging;

public sealed class RabbitMqEventConsumer(
    IOptions<RabbitMqSettings> settings,
    TaskNotificationStream stream,
    ILogger<RabbitMqEventConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            var message = Encoding.UTF8.GetString(args.Body.ToArray());
            var routingKey = args.RoutingKey;

            try
            {
                var notification = CreateNotificationEvent(routingKey, message);
                if (notification is not null)
                {
                    stream.Publish(notification);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to translate RabbitMQ event {RoutingKey} into a notification.", routingKey);
            }

            channel.BasicAck(args.DeliveryTag, false);
            await Task.CompletedTask;
        };

        channel.BasicConsume(config.Queue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private static TaskNotificationEvent? CreateNotificationEvent(string routingKey, string message)
    {
        return routingKey switch
        {
            "tasks.assigned" => CreateAssignedNotification(message),
            "tasks.statusChanged" => CreateStatusChangedNotification(message),
            "tasks.dueSoon" => CreateDueSoonNotification(message),
            "tasks.highPriority" => CreateHighPriorityNotification(message),
            "tasks.overdue" => CreateOverdueNotification(message),
            _ => null
        };
    }

    private static TaskNotificationEvent? CreateAssignedNotification(string message)
    {
        var payload = JsonSerializer.Deserialize<TaskAssignedPayload>(message, JsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AssigneeId))
        {
            return null;
        }

        var text = payload.PreviousAssigneeId is null
            ? $"Przypisano Ci zadanie: {payload.Title}"
            : $"Przypisano Ci zadanie po zmianie osoby odpowiedzialnej: {payload.Title}";

        return new TaskNotificationEvent(
            payload.AssigneeId,
            text,
            payload.TaskId,
            payload.ProjectId,
            "tasks.assigned");
    }

    private static TaskNotificationEvent? CreateStatusChangedNotification(string message)
    {
        var payload = JsonSerializer.Deserialize<TaskStatusChangedPayload>(message, JsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AssigneeId))
        {
            return null;
        }

        return new TaskNotificationEvent(
            payload.AssigneeId,
            $"Status zadania '{payload.Title}' zmieniono z {FormatStatus(payload.PreviousStatus)} na {FormatStatus(payload.Status)}.",
            payload.TaskId,
            payload.ProjectId,
            "tasks.statusChanged");
    }

    private static TaskNotificationEvent? CreateOverdueNotification(string message)
    {
        var payload = JsonSerializer.Deserialize<TaskOverduePayload>(message, JsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AssigneeId))
        {
            return null;
        }

        return new TaskNotificationEvent(
            payload.AssigneeId,
            $"Termin zadania '{payload.Title}' został przekroczony.",
            payload.TaskId,
            payload.ProjectId,
            "tasks.overdue");
    }

    private static TaskNotificationEvent? CreateDueSoonNotification(string message)
    {
        var payload = JsonSerializer.Deserialize<TaskDueSoonPayload>(message, JsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AssigneeId))
        {
            return null;
        }

        return new TaskNotificationEvent(
            payload.AssigneeId,
            $"Termin zadania '{payload.Title}' zbliża się w ciągu 24 godzin.",
            payload.TaskId,
            payload.ProjectId,
            "tasks.dueSoon");
    }

    private static TaskNotificationEvent? CreateHighPriorityNotification(string message)
    {
        var payload = JsonSerializer.Deserialize<TaskHighPriorityPayload>(message, JsonOptions);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AssigneeId))
        {
            return null;
        }

        return new TaskNotificationEvent(
            payload.AssigneeId,
            $"Zadanie '{payload.Title}' ma wysoki priorytet.",
            payload.TaskId,
            payload.ProjectId,
            "tasks.highPriority");
    }

    private static string FormatStatus(int status)
    {
        return status switch
        {
            0 => "Do zrobienia",
            1 => "W trakcie",
            2 => "Ukończone",
            3 => "Zablokowane",
            _ => status.ToString()
        };
    }

    private sealed record TaskAssignedPayload(
        Guid TaskId,
        Guid ProjectId,
        string Title,
        string AssigneeId,
        string? PreviousAssigneeId);

    private sealed record TaskStatusChangedPayload(
        Guid TaskId,
        Guid ProjectId,
        string Title,
        string AssigneeId,
        int PreviousStatus,
        int Status);

    private sealed record TaskOverduePayload(
        Guid TaskId,
        Guid ProjectId,
        string Title,
        string AssigneeId,
        DateTime DueDate);

    private sealed record TaskDueSoonPayload(
        Guid TaskId,
        Guid ProjectId,
        string Title,
        string AssigneeId,
        DateTime DueDate);

    private sealed record TaskHighPriorityPayload(
        Guid TaskId,
        Guid ProjectId,
        string Title,
        string AssigneeId,
        int Priority);
}
