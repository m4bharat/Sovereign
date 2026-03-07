using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class RelationshipTemperatureEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RelationshipTemperatureEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DecayAlerts_Endpoint_Should_Respond()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/relationships/decay-alerts?userId=user-001");

        response.Should().NotBeNull();
    }
}
