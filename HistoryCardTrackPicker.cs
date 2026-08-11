using Godot;
using System;
using CounterTool;

public partial class HistoryCardTrackPicker : TextureRect
{
    [Export] public Button mainButton, threeLapButton, intermissionButton;
    [Export] public Label label;
    public int trackId;
    public bool openPicker;
    [Signal]
    public delegate void PressedButtonEventHandler(int trackId);
    [Signal]
    public delegate void PauseTimerEventHandler();
    public override void _Ready()
    {
        label.Text = "";
        mainButton.Pressed += ButtonOnPressed;
        threeLapButton.Pressed += ThreeLapButtonOnPressed;
        intermissionButton.Pressed += IntermissionButtonOnPressed;
        trackId = GetIndex();
    }

    private void ThreeLapButtonOnPressed()
    {
        EmitSignalPressedButton(trackId);
        EmitSignalPauseTimer();
        HideStuff();
    }
    private void IntermissionButtonOnPressed()
    {
        EmitSignalPressedButton(trackId + 30);
        EmitSignalPauseTimer();
        HideStuff();
    }
    public void HideStuff()
    {
        threeLapButton.Visible = false;
        intermissionButton.Visible = false;
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
            threeLapButton.Visible = true;
            intermissionButton.Visible = true;
        }
    }
}
