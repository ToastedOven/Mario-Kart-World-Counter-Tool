using Godot;
using System;
using System.Linq;
using CounterTool;
using Godot.Collections;

public partial class RecentTrackTracker : VBoxContainer
{
    [Export] private PackedScene recentTrackScene;

    public Array<HistoryCardTrackPicker> recentTracks = new();
    private Array<RecentTrack> recentTrackIcons = new();
    public static RecentTrackTracker instance;
    public double timer = 0;
    public const double recentTrackTimeToAutoVrScan = 7.5;
    [Export] private Label timerLabel;
    [Export] public CheckBox randomCheckbox;
    [Export] private TextureRect comingFrom;
    [Export] private Button comingFromButton;
    [Export] private Control selected;
    [Export] private CheckBox newSessionCheckbox;
    private int comingFromId = -1;
    public string currentMatchInfo;
    public override void _Ready()
    {
        instance = this;
        comingFrom.Texture = null;
        comingFromButton.Pressed += ComingFromButtonOnPressed;
        selected.GetChild(1).QueueFree();
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
    }

    private void ComingFromButtonOnPressed()
    {
        comingFrom.Texture = null;
        comingFromId = -1;
    }

    public void AddTrack(int trackId, bool skipIncoming = false)
    {
        HistoryCardTrackPicker trackButton;
        trackButton = HistoryCardTrackPicker.buttons[trackId % ControlManager.TRACKCOUNT];
        if (trackId >= ControlManager.TRACKCOUNT)
        {
            trackButton.intermissionBool = true;
        }
        AddTrack(trackButton, skipIncoming);
    }
    
    public void AddTrack(HistoryCardTrackPicker trackButton, bool skipIncoming = false)
    {
        if (comingFromId == -1 && !skipIncoming)
        {
            comingFrom.Texture = trackButton.Texture;
            comingFromId = trackButton.trackId;
            TrackSelectionScanner.instance.TestScan();
            return;
        }
        if (recentTracks.Count >= 4)
        {
            return;
        }
        var newTrack = recentTrackScene.Instantiate<RecentTrack>();
        newTrack.myTrack = trackButton;
        if (recentTracks.Count < 3)
        {
            AddChild(newTrack);
        }
        else
        {
            selected.AddChild(newTrack);
        }
        recentTrackIcons.Add(newTrack);
        recentTracks.Add(trackButton);
        if (recentTracks.Count == 4)
        {
            timer = recentTrackTimeToAutoVrScan;
        }
        else if (recentTracks.Count == 3)
        {
            PickPercentageThing.scanningForVotes = true;
        }
    }
    
    public void RemoveTrack(HistoryCardTrackPicker trackButton)
    {
        if (!recentTracks.Contains(trackButton))
        {
            return;
        }
        var oldTrack = recentTrackIcons[recentTracks.IndexOf(trackButton)];
        recentTrackIcons.Remove(oldTrack);
        oldTrack.QueueFree();
        recentTracks.Remove(trackButton);
        if (recentTrackIcons.Count == 3)
        {
            recentTrackIcons[2].Reparent(this);
        }
    }
    public HistoryCardTrackPicker SwapTrack(HistoryCardTrackPicker trackButton, HistoryCardTrackPicker newButton)
    {
        if (!recentTracks.Contains(trackButton))
        {
            return null;
        }
        recentTracks[recentTracks.IndexOf(trackButton)] = newButton;
        return newButton;
    }

    public void ClearTracks()
    {
        foreach (var recentTrack in recentTracks.Duplicate())
            RemoveTrack(recentTrack);
    }

    public override void _Process(double delta)
    {
        if (recentTracks.Count == 4 && comingFromId != -1)
        {
            if (recentTracks.Count == 4)
                timerLabel.Text = $"Confirming in {timer:N0}";
            timer -= delta;
            if (timer <= 0)
            {
                int wasRandom = randomCheckbox.IsPressed() ? 1 : 0;
                int wasNewSession = newSessionCheckbox.IsPressed() ? 1 : 0;
                randomCheckbox.SetPressed(false);
                newSessionCheckbox.SetPressed(false);
                currentMatchInfo = $"Option1>>{recentTracks[0].realTrackId},Option2>>{recentTracks[1].realTrackId},Option3>>{recentTracks[2].realTrackId},Picked>>{recentTracks[3].realTrackId},Random>>{wasRandom},ComingFrom>>{comingFromId},NewSession>>{wasNewSession},{PickPercentageThing.GetCurrentTrackVotesForSaveData(recentTracks[0].realTrackId, recentTracks[1].realTrackId, recentTracks[2].realTrackId)}";
                PostMatchPage.instance.track.Texture = recentTracks[3].Texture;
                PostMatchPage.instance.kart.Texture = SearchBar.instance.karts.GetChild<TextureRect>(ComboButton.currentKart).Texture;
                PostMatchPage.instance.driver.Texture = SearchBar.instance.characters.GetChild<TextureRect>(ComboButton.currentDriver).Texture;
                comingFromId = HistoryCardTrackPicker.buttons.IndexOf(recentTracks.Last()) % ControlManager.TRACKCOUNT;
                switch (comingFromId)
                {
                    case 29:
                        comingFromId = 28;
                        break;
                    case 30:
                    case 31:
                    case 32:
                        comingFromId = 27;
                        break;
                    case 33:
                    case 34:
                    case 35:
                        comingFromId = 20;
                        break;
                    case 36:
                    case 37:
                        comingFromId = 23;
                        break;
                    case 38:
                        comingFromId = 10;
                        break;
                    case 39:
                        comingFromId = 12;
                        break;
                }
                comingFrom.Texture = ControlManager.instance.trackTextures[comingFromId];
                recentTracks.Clear();
                foreach (var track in recentTrackIcons)
                {
                    track.QueueFree();
                }
                recentTrackIcons.Clear();
                ControlManager.instance.postMatchPage.Visible = true;
                PickPercentageThing.GiveUp();
            }
        }
        else
        {
            timerLabel.Text = $"Please finish selections";
        }
    }
}
