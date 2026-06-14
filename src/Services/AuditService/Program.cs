using BuildingBlocks.Aspects;
using BuildingBlocks.Middleware;
using BuildingBlocks.Persistence;
using Consul;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using System.Text;
using AuditService.Consul;
using AuditService.Data;
using AuditService.Messaging;
using AuditService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServerDbContextWithAudit<AuditDbContext>(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMediatR(typeof(Program).Assembly);
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

builder.Services.AddControllersWithApplicationAspects();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

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

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<RabbitMqEventConsumer>();

var app = builder.Build();
AspectExecutionLogger.Configure(app.Services);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    dbContext.Database.Migrate();
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
