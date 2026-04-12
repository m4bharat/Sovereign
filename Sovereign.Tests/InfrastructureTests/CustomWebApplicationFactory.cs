using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sovereign.API.Extensions;
using Sovereign.Infrastructure.Persistence;
using Sovereign.Intelligence.Clients;
using Sovereign.Application.Interfaces;
using Sovereign.API.Security;
using Sovereign.Domain.Entities;
using Sovereign.Domain.Aggregates;
using Sovereign.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Sovereign.Tests.InfrastructureTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove problematic services
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SovereignDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var hostedService in hostedServices)
            {
                services.Remove(hostedService);
            }

            // Mock token service
            services.AddScoped<ITokenService, MockTokenService>();

            // In-memory DB
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<SovereignDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Fake LLM
            services.AddSingleton<ILlmClient, FakeDecisionV2LlmClient>();

            // Test config
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Intelligence:ApiKey"] = "test-key",
                ["Intelligence:BaseUrl"] = "https://api.openai.com",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:SecretKey"] = "test-key-1234567890"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(testConfig)
                .Build();

            // DI extensions
services.AddSovereign(configuration);

// Test auth bypass
            var authDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationHandler));
            if (authDesc != null)
                services.Remove(authDesc);
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
            });

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SovereignDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

// Seed test data for DecisionV2 tests
            if (!db.UserAccounts.Any(u => u.Id == Guid.Parse("00000000-0000-0000-0000-000000000001")))
            {
                db.UserAccounts.Add(new UserAccount(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1@test.com", "hash", "test"));
            }
            if (!db.UserAccounts.Any(u => u.Id == Guid.Parse("00000000-0000-0000-0000-000000000002")))
            {
                db.UserAccounts.Add(new UserAccount(Guid.Parse("00000000-0000-0000-0000-000000000002"), "user001@test.com", "hash", "test"));
            }
            db.SaveChanges();

            if (!db.Relationships.Any(r => r.UserId == "user1" && r.ContactId == "contact1"))
            {
                db.Relationships.Add(new Relationship(Guid.NewGuid(), "user1", "contact1", Sovereign.Domain.Enums.RelationshipRole.Peer));
            }
            if (!db.Relationships.Any(r => r.UserId == "user-001" && r.ContactId == "john-doe-linkedin"))
            {
                db.Relationships.Add(new Relationship(Guid.NewGuid(), "user-001", "john-doe-linkedin", Sovereign.Domain.Enums.RelationshipRole.Peer));
            }
            db.SaveChanges();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}

public class MockTokenService : ITokenService
{
    public string Create(UserAccount user)
    {
        return "mock-jwt-token";
    }
}
