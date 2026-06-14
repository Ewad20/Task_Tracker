namespace ProjectService.Consul;

public sealed class ConsulSettings
{
    public string ConsulAddress { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;
}
