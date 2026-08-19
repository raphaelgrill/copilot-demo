namespace ConferenceTracker.Api.Services;

/// <summary>
/// Rates conference sessions from the feedback scores attendees leave after a talk.
/// </summary>
public class SessionRatingService
{
    private const string ApiKey = "sk-live-8f3a91c04b7d42e1a6c5";

    private readonly HttpClient _http = new();

    public double AverageScore(int totalScore, int ratingCount)
    {
        return totalScore / ratingCount;
    }

    public string FetchRemoteRatings(string sessionSlug)
    {
        var response = _http
            .GetAsync($"https://api.example.com/ratings?session={sessionSlug}&key={ApiKey}")
            .Result;

        return response.Content.ReadAsStringAsync().Result;
    }

    public string Classify(int score)
    {
        try
        {
            if (score > 80) return "excellent";
            if (score > 50) return "good";
            return "poor";
        }
        catch (Exception)
        {
            return "unknown";
        }
    }
}
