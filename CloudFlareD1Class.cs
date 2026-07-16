using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public class CloudflareClient
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _workerUrl;
    private readonly string _apiKey;

    public CloudflareClient(string workerUrl, string apiKey)
    {
        _workerUrl = workerUrl;
        _apiKey = apiKey;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, _workerUrl + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return request;
    }
    
    public async Task<(bool success, string streamId, string pushUrl)> GenerateStreamAsync()
    {
        try
        {
            var response = await _httpClient.SendAsync(CreateRequest(HttpMethod.Post, "stream/generate"));
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            return (
                root.GetProperty("success").GetBoolean(),
                root.GetProperty("streamId").GetString(),
                root.GetProperty("pushUrl").GetString()
            );
        }
        catch { return (false, null, null); }
    }
    public async Task<bool> ActivateStreamAsync(string streamId)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "stream/activate");
            request.Content = JsonContent.Create(new { streamId });
            
            var response = await _httpClient.SendAsync(request);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
        catch { return false; }
    }

    public async Task<bool> StopStreamAsync()
    {
        try
        {
            var response = await _httpClient.SendAsync(CreateRequest(HttpMethod.Post, "stream/stop"));
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
        catch { return false; }
    }
    public async Task<bool> InsertHistoryEntryAsync(HistoryEntry entry)
    {
        // 1. Define standard parameterized query using '?' placeholders
        string sqlQuery = @"
            INSERT INTO history (
                option1, option2, option3, picked, is_random, coming_from, 
                new_session, placement, player_count, friendly_date, 
                racer, kart, my_vr, match_vrs, average_vr, option1_votes, option2_votes, option3_votes, random_votes, timestamp
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";

        // 2. Format the list of integers into a clean JSON string array format for the database
        string matchVrsJson = JsonSerializer.Serialize(entry.MatchVrs);

        // 3. Assemble parameters in the precise chronological order of the placeholders
        object[] queryParams = new object[]
        {
            entry.Option1,
            entry.Option2,
            entry.Option3,
            entry.Picked,
            entry.Random ? 1 : 0, // SQLite treats booleans as 1 (true) or 0 (false)
            entry.ComingFrom,
            entry.NewSession ? 1 : 0,
            entry.Placement,
            entry.PlayerCount,
            entry.Date,
            entry.Racer,
            entry.Kart,
            entry.MyVr,
            matchVrsJson,
            entry.AverageVr,
            entry.Option1Votes,
            entry.Option2Votes,
            entry.Option3Votes,
            entry.RandomVotes,
            entry.Timestamp,
        };

        // 4. Construct the standard payload required by your proxy worker
        var payload = new
        {
            query = sqlQuery,
            @params = queryParams
        };

        // 5. Send payload over HTTP POST with authorization details
        var request = new HttpRequestMessage(HttpMethod.Post, _workerUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(payload);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Match entry recorded successfully in Cloudflare D1!");
                return true;
            }

            string errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Server returned an error: {response.StatusCode} - {errorBody}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Network exception encountered: {ex.Message}");
            return false;
        }
    }
    public async Task<string> CheckIfKeyIsValid(string key)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "verify");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch { return "failed to send request"; }
    }
}