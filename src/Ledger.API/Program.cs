using System.Reflection;
using Ledger.API.Common;
using Ledger.API.Extensions;
using Ledger.Application.Extensions;
using Ledger.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.SetupConfiguration();

builder.AddLogging();
builder.Services.AddObservability();

builder.Services.AddCorsPolicy();

builder.Services.AddApplication(Assembly.Load("Ledger.Application"));

builder.Services.AddInfrastructure(configuration, builder.Environment, Assembly.Load("Ledger.Infrastructure"));

builder.Services.AddEndpoints();
builder.Services.AddOpenApi();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseCors("Development");

app.RegisterMinimalEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Ledger.API V1")
            .WithTheme(ScalarTheme.Purple)
            .HideDarkModeToggle()
            .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios);
    });
}

if (app.Environment.IsProduction())
{
    app.Services.ApplyPendingMigrations();
}

app.UseHttpsRedirection();

try
{
    logger.LogInformation("Ledger.API is starting up");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application start-up failed");
    throw;
}

// Required for integration tests
public partial class Program;
