using Consul;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Gateway.Consul;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

builder.Services.Configure<ConsulSettings>(builder.Configuration.GetSection("Consul"));
builder.Services.AddSingleton<IConsulClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<ConsulSettings>>().Value;
    return new ConsulClient(config =>
    {
        if (!string.IsNullOrWhiteSpace(settings.ConsulAddress))
        {
            config.Address = new Uri(settings.ConsulAddress);
        }
    });
});
builder.Services.AddHostedService<ConsulRegistrationService>();

var app = builder.Build();

app.UseCors("spa");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { status = "Gateway running" }));
app.MapReverseProxy();

app.Run();
