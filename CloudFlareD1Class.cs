using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CounterTool;

public class CloudflareClient(string workerUrl, string apiKey)
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _workerUrl = workerUrl.TrimEnd('/');

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint = "")
    {
        string fullUrl = string.IsNullOrEmpty(endpoint) 
            ? _workerUrl 
            : $"{_workerUrl}/{endpoint.TrimStart('/')}";

        var request = new HttpRequestMessage(method, fullUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    public async Task<bool> InsertHistoryEntryAsync(HistoryEntry entry)
    {
        var payload = new
        {
            option1 = entry.Option1,
            option2 = entry.Option2,
            option3 = entry.Option3,
            picked = entry.Picked,
            is_random = entry.Random ? 1 : 0,
            coming_from = entry.ComingFrom,
            new_session = entry.NewSession ? 1 : 0,
            placement = entry.Placement,
            player_count = entry.PlayerCount,
            friendly_date = entry.Date,
            racer = entry.Racer,
            kart = entry.Kart,
            my_vr = entry.MyVr,
            average_vr = entry.AverageVr,
            match_vrs = entry.MatchVrs,
            option1_votes = entry.Option1Votes,
            option2_votes = entry.Option2Votes,
            option3_votes = entry.Option3Votes,
            random_votes = entry.RandomVotes,
            timestamp = entry.Timestamp,
            disconnected = entry.Disconnect ? 1 : 0
        };

        try
        {
            var request = CreateRequest(HttpMethod.Post);
            request.Content = JsonContent.Create(payload);

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