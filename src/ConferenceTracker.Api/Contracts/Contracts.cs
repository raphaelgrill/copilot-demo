using ConferenceTracker.Api.Domain;

namespace ConferenceTracker.Api.Contracts;

// Rooms
public record RoomResponse(Guid Id, string Name, int Capacity);
public record CreateRoomRequest(string Name, int Capacity);

// Speakers
public record SpeakerResponse(Guid Id, string FullName, string Bio, string Email, string? Twitter);
public record SpeakerSessionResponse(Guid Id, string Title, DateTimeOffset StartsAt);
public record SpeakerDetailResponse(
    Guid Id,
    string FullName,
    string Bio,
    string Email,
    string? Twitter,
    IReadOnlyList<SpeakerSessionResponse> Sessions);
public record SaveSpeakerRequest(string FullName, string Bio, string Email, string? Twitter);

// Sessions
public record SessionListItemResponse(
    Guid Id,
    string Title,
    SessionLevel Level,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string SpeakerName,
    string RoomName);

public record SessionDetailResponse(
    Guid Id,
    string Title,
    string Abstract,
    SessionLevel Level,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string SpeakerName,
    string RoomName,
    int Capacity,
    int RegisteredCount,
    int SeatsLeft);

public record SaveSessionRequest(
    string Title,
    string Abstract,
    SessionLevel Level,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid SpeakerId,
    Guid RoomId);

// Attendees
public record AttendeeResponse(Guid Id, string FullName, string Email);
public record CreateAttendeeRequest(string FullName, string Email);
public record AgendaItemResponse(
    Guid SessionId,
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string RoomName,
    string SpeakerName);

// Registrations
public record CreateRegistrationRequest(Guid AttendeeId);
public record RegistrationResponse(
    Guid SessionId,
    Guid AttendeeId,
    DateTimeOffset RegisteredAt,
    RegistrationStatus Status);
