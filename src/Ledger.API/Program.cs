using System.Reflection;
using Ledger.API.Extensions;
using Ledger.Application.Extensions;
using Ledger.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
builder.Services.AddObservability();

builder.Services.AddApplication(Assembly.Load("Ledger.Application"));

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment, Assembly.Load("Ledger.Infrastructure"));

builder.Services.AddEndpoints();
builder.Services.AddOpenApi();

var app = builder.Build();

app.RegisterMinimalEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsProduction())
{
    app.Services.ApplyPendingMigrations();
}

app.UseHttpsRedirection();
app.Run();

// Required for integration tests
public partial class Program;