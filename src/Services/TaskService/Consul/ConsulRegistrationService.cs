using Consul;
using Microsoft.Extensions.Options;

namespace TaskService.Consul;

public sealed class ConsulRegistrationService(IConsulClient consulClient, IOptions<ConsulSettings> settings) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = settings.Value;
        if (string.IsNullOrWhiteSpace(config.ServiceName))
        {
            return;
        }

        var registration = new AgentServiceRegistration
        {
            ID = config.ServiceId,
            Name = config.ServiceName,
            Address = new Uri(config.ServiceAddress).Host,
            Port = new Uri(config.ServiceAddress).Port,
            Check = new AgentServiceCheck
            {
                HTTP = $"{config.ServiceAddress}/health",
                Interval = TimeSpan.FromSeconds(10)
            }
        };

        try
        {
            await consulClient.Agent.ServiceRegister(registration, cancellationToken);
        }
        catch (Exception)
        {
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var config = settings.Value;
        if (string.IsNullOrWhiteSpace(config.ServiceId))
        {
            return;
        }

        try
        {
            await consulClient.Agent.ServiceDeregister(config.ServiceId, cancellationToken);
        }
        catch (Exception)
        {
        }
    }
}
