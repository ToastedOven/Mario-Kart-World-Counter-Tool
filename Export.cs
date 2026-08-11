using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CounterTool;
using HttpClient = System.Net.Http.HttpClient;

public partial class Export : Button
{
    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    private async void OnPressed()
    {
        var d1Client = new CloudflareClient(SettingsPage.dbUrl, ApiKeyEntry.apiKey);
        int failedCount = 0;
        for (int i = 0; i < HistoryHandler.cards.Count; i++)
        {
            var historyCard = HistoryHandler.cards[i];
            var newMatch = historyCard.GetHistoryInfoForDb();
            var (worked, output) = await d1Client.InsertHistoryEntryAsync(newMatch);
            if (worked)
            {
                HistoryHandler.cards.Remove(historyCard);
                historyCard.QueueFree();
                i--;
            }
            else
            {
                failedCount++;
            }
        }

        if (failedCount > 0)
        {
            ControlManager.instance.CreatePopup($"{failedCount} entries failed to upload, they have been retained.");
        }
        SaveManager.Save();
    }
}

public class HistoryEntry
{
    public string Option1 { get; set; }
    public string Option2 { get; set; }
    public string Option3 { get; set; }
    public string Picked { get; set; }
    public bool Random { get; set; }
    public string ComingFrom { get; set; }
    public bool NewSession { get; set; }
    public int Placement { get; set; }
    public int PlayerCount { get; set; }
    public string Racer { get; set; }
    public string Kart { get; set; }
    public int MyVr { get; set; }
    public List<int> MatchVrs { get; set; } = new List<int>();
    public double AverageVr => MatchVrs.Count > 0 ? (int)MatchVrs.Average() : 0;
    public int Option1Votes { get; set; }
    public int Option2Votes { get; set; }
    public int Option3Votes { get; set; }
    public int RandomVotes { get; set; }
    public long Timestamp { get; set; }
    public bool Disconnect { get; set; }
    public string Version { get; set; }
    public bool Mirror { get; set; }
}