using Godot;
using System;
using CounterTool;

public partial class PostMatchPage : Control
{
    public static PostMatchPage  instance;
    [Export] public TextureRect track, driver, kart;
    public bool disconnected;
    public override void _Ready()
    {
        instance = this;
        ControlManager.instance.dcButton.Pressed += DcButtonOnPressed;
    }

    private async void DcButtonOnPressed()
    {
        disconnected = true;
        PositionButton.currentRacePlayerCount = 1;
        PositionButton.currentRacePosition = 1;
        await PositionButton.FinalizeRace();
    }
}
