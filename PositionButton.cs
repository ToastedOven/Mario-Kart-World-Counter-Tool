using Godot;
using System;
using CounterTool;

public partial class PositionButton : TextureRect
{
    [Export] private Button button;
    [Export] private CheckBox hideTimeStamp;
    public static int currentRacePosition = -1;
    public static int currentRacePlayerCount;
    [Export] private Label instructions;
    public override void _Ready()
    {
        button.Pressed += ButtonOnPressed;
    }

    private async void ButtonOnPressed()
    {
        if (currentRacePosition == -1)
        {
            currentRacePosition = GetIndex();
            instructions.Text = "Pick player count";
        }
        else
        {
            currentRacePlayerCount = GetIndex();
            instructions.Text = "Pick your placement";
            string matchTime = "Hidden";
            string timeStamp = "Hidden";
            if (!hideTimeStamp.IsPressed())
            {
                string tzId = TimeZoneInfo.Local.Id;
                if (!TimeZoneInfo.Local.HasIanaId && TimeZoneInfo.TryConvertWindowsIdToIanaId(tzId, out var ianaId))
                {
                    tzId = ianaId;
                }
                matchTime = $"{DateTime.Now.ToString("[MM/dd/yyyy h:mmtt")} {tzId}]";
                timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }

            
            string matchInfo = $"{RecentTrackTracker.instance.currentMatchInfo},Position>>{currentRacePosition},PlayerCount>>{currentRacePlayerCount},Date>>{matchTime},Timestamp>>{timeStamp},DriverIndex>>{ComboButton.currentDriver},KartIndex>>{ComboButton.currentKart}";
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
            if (ControlManager.instance.autoUpload.IsPressed())
            {
                var d1Client = new CloudflareClient("https://nunchuk-db-proxy.dwelxs2.workers.dev/", ApiKeyEntry.apiKey);
                await d1Client.InsertHistoryEntryAsync(newMatch);   
                HistoryHandler.instance.RemoveLatestCard();
            }
            SaveManager.Save();
            currentRacePosition = -1;
        }
    }
}
