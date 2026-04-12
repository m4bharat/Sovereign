using FluentAssertions;
using Sovereign.Tests.InfrastructureTests;
using Sovereign.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sovereign.Tests.APIEndpointTests;

public sealed class RewriteEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RewriteEndpointTests(CustomWebApplicationFactory factory)
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

        var payload = await response.Content.ReadFromJsonAsync<RewriteMessageResponse>();
        payload.Should().NotBeNull();
        payload!.Variants.Should().NotBeNull();
        payload.Variants.Should().NotBeEmpty();
    }
}