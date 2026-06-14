using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ProjectService.Messaging;

public interface IEventPublisher
{
    void Publish<T>(string eventName, T payload);
}

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqSettings> settings) : IEventPublisher
{
    public void Publish<T>(string eventName, T payload)
    {
        var config = settings.Value;
        var factory = new ConnectionFactory { HostName = config.Host };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(config.Exchange, ExchangeType.Topic, durable: true);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        channel.BasicPublish(config.Exchange, eventName, null, body);
    }
}
