using Godot;
using System;
using CounterTool;
using Godot.Collections;

public partial class HistoryCardTrackPicker : TextureRect
{
    [Export] public Button mainButton, threeLapButton, intermissionButton;
    [Export] public Label label;
    public int trackId;
    public bool intermissionBool;
    public int realTrackId => intermissionBool ? trackId + ControlManager.TRACKCOUNT : trackId;
    public bool openPicker;
    [Signal]
    public delegate void PressedButtonEventHandler(int trackId);
    [Signal]
    public delegate void PauseTimerEventHandler();
    public static Array<HistoryCardTrackPicker> buttons = new();
    [Export] private ColorRect overlay;
    public override void _Ready()
    {
        label.Text = "";
        mainButton.Pressed += ButtonOnPressed;
        threeLapButton.Pressed += ThreeLapButtonOnPressed;
        intermissionButton.Pressed += IntermissionButtonOnPressed;
        trackId = GetIndex();
        buttons.Add(this);
        SetDimmed(false);
    }

    private void ThreeLapButtonOnPressed()
    {
        intermissionBool = false;
        EmitSignalPressedButton(trackId);
        EmitSignalPauseTimer();
        HideStuff();
    }
    private void IntermissionButtonOnPressed()
    {
        intermissionBool = true;
        EmitSignalPressedButton(trackId + ControlManager.TRACKCOUNT);
        EmitSignalPauseTimer();
        HideStuff();
    }
    public void HideStuff()
    {
        SetOptionsVisibility(false);
        ControlManager.instance.trackSelectionPage.Visible = false;
    }


    private void ButtonOnPressed()
    {
        EmitSignalPauseTimer();
        if (openPicker)
        {
            HistoryCardEditor.currentTrackPickerCard = this;
            ControlManager.instance.trackSelectionPage.Visible = true;
        }
        else
        {
            SetOptionsVisibility(true);
        }
    }

    public void SetOptionsVisibility(bool visible)
    {
        threeLapButton.Visible = visible;
        intermissionButton.Visible = visible;
    }
    public void SetDimmed(bool dimmed)
    {
        if (dimmed)
        {
            overlay.Color = new Color(0, 0, 0, .66f);
        }
        else
        {
            overlay.Color = new Color(0, 0, 0, 0);
        }
    }
}
