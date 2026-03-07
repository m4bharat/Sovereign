using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Sovereign.Application.DTOs;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class AiDecisionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AiDecisionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AiDecision_Should_Return_Ok()
    {
        var client = _factory.CreateClient();

        var request = new AiDecisionRequest
        {
            UserId = "user-001",
            ContactId = "contact-001",
            Message = "Remember that my birthday is Jan 10",
            RelationshipRole = "Friend"
        };

        var response = await client.PostAsJsonAsync("/api/ai/decide", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
