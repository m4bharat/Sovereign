using FluentAssertions;
using Sovereign.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sovereign.API.IntegrationTests;

public sealed class ConversationThreadEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConversationThreadEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateThread_Should_Return_Ok()
    {
        var client = _factory.CreateClient();

        var request = new CreateThreadRequest
        {
            UserId = "user-001",
            ContactId = "contact-001",
            Title = "Investor Follow Up"
        };

        var response = await client.PostAsJsonAsync("/api/conversations/threads", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CreateThreadResponse>();
        payload.Should().NotBeNull();
        payload!.UserId.Should().Be("user-001");
        payload.ContactId.Should().Be("contact-001");
    }
}