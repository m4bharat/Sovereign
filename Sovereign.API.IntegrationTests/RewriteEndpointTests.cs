using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Sovereign.Application.DTOs;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class RewriteEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RewriteEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rewrite_Should_Return_Ok()
    {
        var client = _factory.CreateClient();

        var request = new RewriteMessageRequest
        {
            UserId = "user-001",
            ContactId = "contact-001",
            Draft = "hey can we meet sometime",
            RelationshipRole = "Investor",
            Goal = "ScheduleMeeting",
            Platform = "LinkedIn"
        };

        var response = await client.PostAsJsonAsync("/api/ai/rewrite", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
