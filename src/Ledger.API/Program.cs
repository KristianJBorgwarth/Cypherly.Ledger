using System.Reflection;
using Ledger.API.Extensions;
using Ledger.Application.Abstractions;
using Ledger.Application.Behavior;
using Ledger.Application.Extensions;
using Ledger.Domain.Abstractions;
using Ledger.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
builder.Services.AddObservability();

builder.Services.AddApplication(Assembly.Load("Ledger.Application"));

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment, Assembly.Load("Ledger.Infrastructure"));

// Options are read by the source generator at compile time, so they must stay inline constants
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Assemblies = [typeof(ICommand), typeof(IDomainEvent)];
    options.PipelineBehaviors = [typeof(ValidationBehavior<,>), typeof(ExceptionBehavior<,>)];
});

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