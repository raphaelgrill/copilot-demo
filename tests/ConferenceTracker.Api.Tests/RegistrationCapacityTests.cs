using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConferenceTracker.Api.Contracts;
using ConferenceTracker.Api.Domain;
using ConferenceTracker.Api.Tests.Infrastructure;

namespace ConferenceTracker.Api.Tests;

/// <summary>
/// The room-capacity rule end to end. Session 5 sits in the 8-seat Fireside Corner with 7 seeded
/// registrations, so it is exactly one seat away from being full.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class RegistrationCapacityTests : IAsyncLifetime
{
    private static readonly Guid FiresideSession = new("0c000000-0000-0000-0000-000000000005");
    private static readonly Guid AttendeeEight = new("0d000000-0000-0000-0000-000000000008");
    private static readonly Guid AttendeeNine = new("0d000000-0000-0000-0000-000000000009");
    private static readonly Guid AttendeeOne = new("0d000000-0000-0000-0000-000000000001");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ConferenceApiFactory _factory;
    private HttpClient _client = null!;

    public RegistrationCapacityTests(PostgresFixture postgres) => _factory = new ConferenceApiFactory(postgres);

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Seed_leaves_exactly_one_seat_in_the_fireside_corner()
    {
        var session = await GetSessionAsync();

        Assert.Equal("Fireside Corner", session.RoomName);
        Assert.Equal(8, session.Capacity);
        Assert.Equal(7, session.RegisteredCount);
        Assert.Equal(1, session.SeatsLeft);
    }

    [Fact]
    public async Task Registering_for_the_last_seat_succeeds()
    {
        var response = await RegisterAsync(AttendeeEight);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await ReadAsync<RegistrationResponse>(response);
        Assert.Equal(FiresideSession, created.SessionId);
        Assert.Equal(AttendeeEight, created.AttendeeId);
        Assert.Equal(RegistrationStatus.Confirmed, created.Status);

        Assert.Equal(0, (await GetSessionAsync()).SeatsLeft);
    }

    [Fact]
    public async Task Registering_for_a_full_session_is_rejected()
    {
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(AttendeeEight)).StatusCode);

        var response = await RegisterAsync(AttendeeNine);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Session is full", await response.Content.ReadAsStringAsync());

        Assert.Equal(0, (await GetSessionAsync()).SeatsLeft);
    }

    [Fact]
    public async Task Cancelling_frees_the_seat()
    {
        await RegisterAsync(AttendeeEight);

        var cancel = await _client.DeleteAsync(
            $"/api/sessions/{FiresideSession}/registrations/{AttendeeEight}");

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(1, (await GetSessionAsync()).SeatsLeft);

        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(AttendeeNine)).StatusCode);
    }

    [Fact]
    public async Task Re_registering_after_a_cancellation_reuses_the_existing_row()
    {
        await RegisterAsync(AttendeeEight);
        var countAfterFirstRegistration = (await GetRegistrationsAsync()).Count;

        await _client.DeleteAsync($"/api/sessions/{FiresideSession}/registrations/{AttendeeEight}");

        var again = await RegisterAsync(AttendeeEight);

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal(countAfterFirstRegistration, (await GetRegistrationsAsync()).Count);

        var registrations = await GetRegistrationsAsync();
        var reused = Assert.Single(registrations, r => r.AttendeeId == AttendeeEight);
        Assert.Equal(RegistrationStatus.Confirmed, reused.Status);
    }

    [Fact]
    public async Task Registering_an_already_confirmed_attendee_is_rejected()
    {
        var response = await RegisterAsync(AttendeeOne);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already registered", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Registering_for_an_unknown_session_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/sessions/{Guid.NewGuid()}/registrations",
            new CreateRegistrationRequest(AttendeeEight));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Registering_an_unknown_attendee_returns_not_found()
    {
        var response = await RegisterAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> RegisterAsync(Guid attendeeId) =>
        _client.PostAsJsonAsync(
            $"/api/sessions/{FiresideSession}/registrations",
            new CreateRegistrationRequest(attendeeId));

    private async Task<SessionDetailResponse> GetSessionAsync()
    {
        var response = await _client.GetAsync($"/api/sessions/{FiresideSession}");
        response.EnsureSuccessStatusCode();
        return await ReadAsync<SessionDetailResponse>(response);
    }

    private async Task<List<RegistrationResponse>> GetRegistrationsAsync()
    {
        var response = await _client.GetAsync($"/api/sessions/{FiresideSession}/registrations");
        response.EnsureSuccessStatusCode();
        return await ReadAsync<List<RegistrationResponse>>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.NotNull(payload);
        return payload;
    }
}
