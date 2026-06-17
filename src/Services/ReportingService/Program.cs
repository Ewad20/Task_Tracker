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
using ReportingService.Consul;
using ReportingService.Data;
using ReportingService.Messaging;
using ReportingService.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServerDbContextWithAudit<ReportingDbContext>(builder.Configuration);

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
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<RabbitMqReportConsumer>();

builder.Services.AddControllersWithApplicationAspects();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "ReportingService API", Version = "v1" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Wpisz token JWT (bez prefiksu 'Bearer')"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

var app = builder.Build();
AspectExecutionLogger.Configure(app.Services);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    dbContext.Database.Migrate();
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
