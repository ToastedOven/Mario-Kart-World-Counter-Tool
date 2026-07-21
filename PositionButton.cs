using Godot;
using System;
using System.Threading.Tasks;
using CounterTool;

public partial class PositionButton : TextureRect
{
    [Export] private Button button;
    [Export] private CheckBox hideTimeStamp;
    public static int currentRacePosition = -1;
    public static int currentRacePlayerCount;
    public override void _Ready()
    {
        button.Pressed += ButtonOnPressed;
    }

    private async void ButtonOnPressed()
    {
        if (currentRacePosition == -1)
        {
            currentRacePosition = GetIndex();
            ControlManager.instance.postMatchInstructions.Text = "Pick player count";
            if (VRAverageCalculator.currentMatchVRs.Count > 0)
            {
                currentRacePlayerCount = VRAverageCalculator.currentMatchVRs.Count;
                await FinalizeRace();
            }
        }
        else
        {
            currentRacePlayerCount = GetIndex();
            await FinalizeRace();
        }
    }

    public static async Task FinalizeRace()
    {
        ControlManager.instance.postMatchInstructions.Text = "Pick your placement";
        string matchTime = "Hidden";
        string timeStamp = "Hidden";
        if (!SettingsPage.hiddenTimestamp)
        {
            string tzId = TimeZoneInfo.Local.Id;
            if (!TimeZoneInfo.Local.HasIanaId && TimeZoneInfo.TryConvertWindowsIdToIanaId(tzId, out var ianaId))
            {
                tzId = ianaId;
            }
            matchTime = $"{DateTime.Now:[MM/dd/yyyy h:mmtt} {tzId}]";
            timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

            
        string matchInfo = $"{RecentTrackTracker.instance.currentMatchInfo},Position>>{currentRacePosition},PlayerCount>>{currentRacePlayerCount},Date>>{matchTime},Timestamp>>{timeStamp},DriverIndex>>{ComboButton.currentDriver},KartIndex>>{ComboButton.currentKart},Disconnected>>{(PostMatchPage.instance.disconnected ? 1 : 0)}";
        if (VRAverageCalculator.currentMatchVRs.Count != 0)
        {
            matchInfo += $",MyVR>>{VRAverageCalculator.myCurrentVR},MatchVRs>>";
            foreach (var player in VRAverageCalculator.currentMatchVRs)
            {
                matchInfo += $"{player}?";
            }
            matchInfo = matchInfo.TrimEnd('?');
            VRAverageCalculator.instance.Reset();
        }
        var historyCard = HistoryHandler.instance.CreateCard(matchInfo);
        var newMatch = historyCard.GetHistoryInfoForDb();
            
        ControlManager.instance.postMatchPage.Visible = false;
        if (SettingsPage.autoUpload)
        {
            var d1Client = new CloudflareClient(SettingsPage.dbUrl, ApiKeyEntry.apiKey);
            await d1Client.InsertHistoryEntryAsync(newMatch);   
            HistoryHandler.instance.RemoveLatestCard();
        }
        SaveManager.Save();
        currentRacePosition = -1;
    }
}
