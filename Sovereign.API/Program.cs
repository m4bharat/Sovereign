using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Sovereign.API.Middleware;
using Sovereign.Application;
using Sovereign.Infrastructure;
using Sovereign.Intelligence.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

//builder.Services
//    .AddControllers()
//    .AddFluentValidation(config =>
//    {
//        config.RegisterValidatorsFromAssemblyContaining<Program>();
//        config.DisableDataAnnotationsValidation = true;
//    });
// 1. Register the Validators (this finds your classes like CreateRelationshipValidator)

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 2. Enable Auto-Validation and/or Client-side adapters
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSovereignIntelligence(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program { }
