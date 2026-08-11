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
                currentRacePlayerCount = VRAverageCalculator.currentMatchVRs.Count - 1;
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
        string timeStamp = "-1";
        if (!SettingsPage.hiddenTimestamp)
        {
            string tzId = TimeZoneInfo.Local.Id;
            if (!TimeZoneInfo.Local.HasIanaId && TimeZoneInfo.TryConvertWindowsIdToIanaId(tzId, out var ianaId))
            {
                tzId = ianaId;
            }
            timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

            
        string matchInfo = $"{RecentTrackTracker.instance.currentMatchInfo},Position>>{currentRacePosition + 1},PlayerCount>>{currentRacePlayerCount + 1},Timestamp>>{timeStamp},DriverIndex>>{ComboButton.currentDriver},KartIndex>>{ComboButton.currentKart},Disconnected>>{(PostMatchPage.instance.disconnected ? 1 : 0)},Version>>{SettingsPage.currentVersion},Mirror>>{(ControlManager.instance.mirrorMode.IsPressed() ? 1 : 0)}";
        ControlManager.instance.mirrorMode.SetPressed(false);
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
        VerifyDataPage.instance.SetupHistoryCard(matchInfo);
        ControlManager.instance.postMatchPage.Visible = false;
    }
}
