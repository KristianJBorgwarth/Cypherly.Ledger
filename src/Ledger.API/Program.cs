using Ledger.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
builder.Services.AddObservability();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();

// Required for integration tests
public partial class Program;