using Godot;
using System;
using System.Linq;
using CounterTool;
using Godot.Collections;

public partial class RecentTrackTracker : VBoxContainer
{
    [Export] private PackedScene recentTrackScene;

    public Array<ButtonThing> recentTracks = new();
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

    public void AddTrack(int trackId)
    {
        AddTrack(ButtonThing.buttons[trackId]);
    }
    public void AddTrack(ButtonThing trackButton)
    {
        if (comingFromId == -1)
        {
            comingFrom.Texture = trackButton.Texture;
            comingFromId = ButtonThing.buttons.IndexOf(trackButton) % 30;
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
    
    public void RemoveTrack(ButtonThing trackButton)
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
    public ButtonThing SwapTrack(ButtonThing trackButton, ButtonThing newButton)
    {
        if (!recentTracks.Contains(trackButton))
        {
            return null;
        }
        recentTracks[recentTracks.IndexOf(trackButton)] = newButton;
        return newButton;
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
                for (int i = 0; i < 3; i++)
                {
                    recentTracks[i].SetOfferedValue(recentTracks[i].offeredValue + 1, true);
                }
                recentTracks[3].SetPickedValue(recentTracks[3].pickedValue + 1, true);
                int wasRandom = randomCheckbox.IsPressed() ? 1 : 0;
                int wasNewSession = newSessionCheckbox.IsPressed() ? 1 : 0;
                randomCheckbox.SetPressed(false);
                newSessionCheckbox.SetPressed(false);
                currentMatchInfo = $"Option1>>{ButtonThing.buttons.IndexOf(recentTrackIcons[0].myTrack)},Option2>>{ButtonThing.buttons.IndexOf(recentTrackIcons[1].myTrack)},Option3>>{ButtonThing.buttons.IndexOf(recentTrackIcons[2].myTrack)},Picked>>{ButtonThing.buttons.IndexOf(recentTracks[3])},Random>>{wasRandom},ComingFrom>>{comingFromId},NewSession>>{wasNewSession},{PickPercentageThing.GetCurrentTrackVotesForSaveData(ButtonThing.buttons.IndexOf(recentTracks[0]), ButtonThing.buttons.IndexOf(recentTracks[1]), ButtonThing.buttons.IndexOf(recentTracks[2]))}";
                PostMatchPage.instance.track.Texture = recentTracks[3].Texture;
                PostMatchPage.instance.kart.Texture = SearchBar.instance.karts.GetChild<TextureRect>(ComboButton.currentKart).Texture;
                PostMatchPage.instance.driver.Texture = SearchBar.instance.characters.GetChild<TextureRect>(ComboButton.currentDriver).Texture;
                comingFromId = ButtonThing.buttons.IndexOf(recentTracks.Last()) % 30;
                if (comingFromId == 29)
                {
                    comingFromId = 28;
                }
                comingFrom.Texture = ButtonThing.buttons[comingFromId].Texture;
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
