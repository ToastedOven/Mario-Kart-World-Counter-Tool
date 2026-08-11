using Godot;
using System;
using System.Text;
using CounterTool;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class ButtonThing : TextureRect
{
    [Export] private Button addButton;
    private Label threeLapsTotalLabel, intermissionsTotalLabel;
    public int offeredValue{ get; private set; }
    public int pickedValue{ get; private set; }
    public static int totalValue => threeLapTotal + intermissionTotal;
    public static int threeLapTotal = 0;
    public static int intermissionTotal = 0;
    
    public static int totalPickedValue => threeLapPickedTotal + intermissionPickedTotal;
    public static int threeLapPickedTotal = 0;
    public static int intermissionPickedTotal = 0;
    
    public static int highestValue = 1;
    public static int highestPickedValue = 1;
    public static Array<ButtonThing> buttons = new();
    public bool intermissionButton = false;
    [Export] private ColorRect overlay;
    public static ButtonThing buttonForEvents;
    [Signal]
    public delegate void RecalculateNeededEventHandler();

    public override void _Ready()
    {
        base._Ready();
        if (buttonForEvents is null)
        {
            buttonForEvents = this;
        }
        addButton.Pressed += ButtonPressed;

        if (buttons.Count == 0)
        {
            LoadAfterFrame();
        }

        if (buttons.Count < 30)
        {
            intermissionButton = false;
        }
        buttons.Add(this);
    }

    async void DelayedSetup()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        threeLapsTotalLabel = SearchBar.instance.threeLapsTotalLabel;
        intermissionsTotalLabel = SearchBar.instance.intermissionsTotalLabel;
    }

    private void ButtonPressed()
    {
        if (Input.IsActionJustReleased("leftClick"))
        {
            AddButtonOnPressed();
        }
        else
        {
            SubtractButtonOnPressed();
        }
    }

    async void LoadAfterFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SaveManager.Load();
        RecalculateButtonPercentages();
        SaveManager.Save();
    }

    private void SubtractButtonOnPressed()
    {
        RecentTrackTracker.instance.RemoveTrack(this);
    }

    private void AddButtonOnPressed()
    {
        RecentTrackTracker.instance.AddTrack(this);
    }

    public void SetOfferedValue(int newValue, bool recalc)
    {
        newValue = Mathf.Max(newValue, 0);
        if (intermissionButton)
        {
            intermissionTotal += newValue - offeredValue;
        }
        else
        {
            threeLapTotal += newValue - offeredValue;
        }
        offeredValue = newValue;
        if (intermissionButton)
        {
            highestValue = Mathf.Max(offeredValue + buttons[buttons.IndexOf(this) - 30].offeredValue, highestValue);
        }
        else
        {
            highestValue = Mathf.Max(offeredValue + buttons[buttons.IndexOf(this) + 30].offeredValue, highestValue);
        }
        if (recalc)
        {
            RecalculateButtonPercentages();
        }
    }
    public void SetPickedValue(int newValue, bool recalc)
    {
        newValue = Mathf.Max(newValue, 0);
        if (intermissionButton)
        {
            intermissionPickedTotal += newValue - pickedValue;
        }
        else
        {
            threeLapPickedTotal += newValue - pickedValue;
        }
        pickedValue = newValue;
        if (intermissionButton)
        {
            highestPickedValue = Mathf.Max(pickedValue + buttons[buttons.IndexOf(this) - 30].pickedValue, highestPickedValue);
        }
        else
        {
            highestPickedValue = Mathf.Max(pickedValue + buttons[buttons.IndexOf(this) + 30].pickedValue, highestPickedValue);
        }
        if (recalc)
        {
            RecalculateButtonPercentages();
        }
    }

    public async void RecalculateButtonPercentages()
    {
        while (!IsInstanceValid(threeLapsTotalLabel))
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        
        threeLapsTotalLabel.Text = $"3Laps Offered: {threeLapTotal} ({(threeLapTotal / (float)totalValue):P1})";
        intermissionsTotalLabel.Text = $"Intermissions Offered: {intermissionTotal} ({(intermissionTotal / (float)totalValue):P1})";
        buttonForEvents.EmitSignalRecalculateNeeded();
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
