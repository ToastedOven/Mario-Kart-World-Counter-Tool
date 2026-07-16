using Godot;
using System;
using CounterTool;

public partial class PageSwitcher : Button
{
    [Export] public Label totalTracksOffered, currentHoveredTrackInfo;
    [Export] public TextureRect currentHoveredTrackTexture;
    public static PageSwitcher instance;

    public override void _Ready()
    {
        base._Ready();
        ControlManager.instance.pickerPage.Visible = false;
        ControlManager.instance.historyPage.Visible = false;
        ControlManager.instance.comboPage.Visible = true;
        Pressed += OnPressed;
        ButtonThing.buttonForEvents.RecalculateNeeded += ButtonForEventsOnRecalculateNeeded;
        instance = this;
        currentHoveredTrackInfo.Text = "";
        ControlManager.instance.historyButton.Pressed += HistoryButtonOnPressed;
        ControlManager.instance.cameraSetup.Pressed += CameraSetupOnPressed;
        ControlManager.instance.enterApiKey.Pressed += () => { ControlManager.instance.apiPage.Visible = true; };
    }

    private void CameraSetupOnPressed()
    {
        ControlManager.instance.cameraPage.Visible = true;
        CameraSetup.instance.Preview();
    }


    private void ButtonForEventsOnRecalculateNeeded()
    {
        if (Bar.checkOffered)
        {
            totalTracksOffered.Text = $"Total Tracks Offered: {ButtonThing.totalValue}";
        }
        else
        {
            totalTracksOffered.Text = $"Total Tracks Picked: {ButtonThing.totalPickedValue}";
        }
    }

    private void OnPressed()
    {
        ControlManager.instance.pickerPage.Visible = true;
        ControlManager.instance.historyPage.Visible = false;
    }
    private void HistoryButtonOnPressed()
    {
        ControlManager.instance.historyPage.Visible = true;
        ControlManager.instance.pickerPage.Visible = false;
    }
}
