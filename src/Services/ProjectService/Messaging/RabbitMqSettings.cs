namespace ProjectService.Messaging;

public sealed class RabbitMqSettings
{
    public string Host { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
}
