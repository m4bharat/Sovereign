using FluentAssertions;
using Sovereign.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class RelationshipTemperatureEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RelationshipTemperatureEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DecayAlerts_Endpoint_Should_Return_Ok()
    {
        var client = _factory.CreateClient();

        var createRequest = new CreateRelationshipRequest
        {
            UserId = "user-001",
            ContactId = "contact-001",
            Role = Sovereign.Domain.Enums.RelationshipRole.Investor
        };

        var createResponse = await client.PostAsJsonAsync("/api/relationships", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync("/api/relationships/decay-alerts?userId=user-001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DecayAlertsResponse>();
        payload.Should().NotBeNull();
    }
}