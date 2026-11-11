using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SecureLab.Api.Data;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Tests;

public sealed class IncidentEndpointTests(SecureLabApiFactory factory)
    : IClassFixture<SecureLabApiFactory>
{
    private readonly HttpClient _anonClient = factory.CreateClient();

    private static string GetSeedPassword()
    {
        return Environment.GetEnvironmentVariable("SeedUsers__Password")
            ?? throw new InvalidOperationException("SeedUsers__Password is not set");
    }

    [Fact]
    public async Task GetList_ReturnsSeededIncidents()
    {
        var password = GetSeedPassword();
        using var client = await factory.LoginAsync("alice", password);

        var incidents = await client.GetFromJsonAsync<List<IncidentListItemResponse>>(
            "/api/incidents");

        Assert.NotNull(incidents);
        Assert.Contains(incidents, incident => incident.Id == DbSeeder.AliceIncidentId);
    }

    [Fact]
    public async Task GetDetails_ForUnknownId_ReturnsProblemDetails404()
    {
        var password = GetSeedPassword();
        using var client = await factory.LoginAsync("alice", password);

        using var response = await client.GetAsync($"/api/incidents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDetails_DoesNotExposeInternalOwnerFields()
    {
        var password = GetSeedPassword();
        using var client = await factory.LoginAsync("alice", password);

        using var response = await client.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(document.RootElement.TryGetProperty("ownerUserId", out _));
        Assert.False(document.RootElement.TryGetProperty("email", out _));
        Assert.Equal("Аліса Коваль", document.RootElement.GetProperty("ownerDisplayName").GetString());
    }

    [Fact]
    public async Task ClientScript_DoesNotUseDangerousInnerHtmlSink()
    {
        var script = await _anonClient.GetStringAsync("/app.js");

        Assert.DoesNotContain("innerHTML", script, StringComparison.Ordinal);
        Assert.Contains("textContent", script, StringComparison.Ordinal);
    }

    // --- НОВІ АВТОРИЗАЦІЙНІ ТЕСТИ ЛР4 ---

    // A-03: Bob намагається прочитати інцидент Alice -> отримує 404 Not Found (політика приховування)
    [Fact]
    public async Task GetDetails_BobCannotReadAliceIncident()
    {
        var password = GetSeedPassword();
        using var client = await factory.LoginAsync("bob", password);

        using var response = await client.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // A-03R: Morgan (Analyst) читає інцидент Alice -> отримує 200 OK (дозволено для аналізу)
    [Fact]
    public async Task GetDetails_AnalystCanReadAliceIncident()
    {
        var password = GetSeedPassword();
        using var client = await factory.LoginAsync("morgan", password);

        using var response = await client.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // A-01: Анонімний користувач намагається прочитати інцидент -> отримує 401 Unauthorized
    [Fact]
    public async Task GetDetails_AnonUser_Returns401()
    {
        using var response = await _anonClient.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}