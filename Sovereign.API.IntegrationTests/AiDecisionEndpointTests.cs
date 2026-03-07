using FluentAssertions;
using Sovereign.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class AiDecisionEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AiDecisionEndpointTests(CustomWebApplicationFactory factory)
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
            RelationshipRole = "Friend",
            RecentSummary = string.Empty,
            LastTopicSummary = string.Empty
        };

        var response = await client.PostAsJsonAsync("/api/ai/decide", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }
}