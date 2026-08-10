using System.Text.Json.Serialization;
using ConferenceTracker.Api.Data;
using ConferenceTracker.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ConferenceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Conference")));

// Enums travel as strings ("Advanced") in JSON and in the OpenAPI schema.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ConferenceDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.MapRoomEndpoints();
app.MapSpeakerEndpoints();
app.MapSessionEndpoints();
app.MapAttendeeEndpoints();
app.MapRegistrationEndpoints();

app.Run();

// Top-level statements generate an internal Program class; the integration tests need
// WebApplicationFactory<Program> to see it.
public partial class Program;
