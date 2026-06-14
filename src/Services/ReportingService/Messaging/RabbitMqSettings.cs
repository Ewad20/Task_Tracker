namespace ReportingService.Messaging;

public sealed class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public string Exchange { get; set; } = "tasktracker.events";
    public string Queue { get; set; } = "reportingservice";
}
