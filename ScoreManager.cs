using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace RideTheSound;

public class ScoreManager
{
    private const string board = "test123";
    private const int clientKey = 3906;

    private readonly HttpClient httpClient;

    public ScoreManager(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<string> GetToken()
    {
        try
        {
            return (await httpClient.GetFromJsonAsync<string>($"https://kristoffer-strube.dk/API/scoreboard/{board}/token"))!;
        }
        catch (Exception)
        {
            return "";
        }
    }

    public async Task<Placement?> SubmitScore(Play play)
    {
        play.Hash = play.CalculateHash(clientKey);
        var response = await httpClient.PostAsJsonAsync($"https://kristoffer-strube.dk/API/scoreboard/{board}", play);
        return await response.Content.ReadFromJsonAsync<Placement>();
    }
}

public class Token
{
    public required string Value { get; set; }
    public required DateTimeOffset Timeout { get; set; }
}

public class Play
{
    public required string User { get; set; }
    public required int Score { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string? Token { get; set; }
    public int Hash { get; set; }

    public int CalculateHash(int clientkey)
    {
        return SHA256.HashData(Encoding.ASCII.GetBytes(clientkey + User + Score + EndTime.ToUnixTimeSeconds() + StartTime.ToUnixTimeSeconds() + Token)).Select(b => (int)b).Sum();
    }
}

public class Placement
{
    public required int Score { get; set; }
    public required int Position { get; set; }
    public required decimal BetterThanPercent { get; set; }
    public required Play[] FiveBetter { get; set; }
    public required Play[] FiveWorse { get; set; }

    public IEnumerable<Play> SelfInline(Play self) => FiveBetter.Append(self).Concat(FiveWorse);
}