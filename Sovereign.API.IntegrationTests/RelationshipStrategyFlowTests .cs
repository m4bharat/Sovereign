using FluentAssertions;
using Sovereign.Application.DTOs;
using Sovereign.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class RelationshipStrategyFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RelationshipStrategyFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Relationship_Temperature_Should_Return_Ok()
    {
        var client = _factory.CreateClient();

        var createRequest = new CreateRelationshipRequest
        {
            UserId = "user-002",
            ContactId = "contact-002",
            Role = RelationshipRole.HiringManager
        };

        var createResponse = await client.PostAsJsonAsync("/api/relationships", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRelationshipResponse>();
        created.Should().NotBeNull();

        var response = await client.GetAsync($"/api/relationships/{created!.RelationshipId}/temperature");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<RelationshipTemperatureResponse>();
        payload.Should().NotBeNull();
        payload!.RelationshipId.Should().Be(created.RelationshipId);
    }
}