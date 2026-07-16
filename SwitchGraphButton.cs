using Godot;
using System;

public partial class SwitchGraphButton : Button
{
    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    private void OnPressed()
    {
        Bar.checkOffered = !Bar.checkOffered;
        ButtonThing.buttonForEvents.RecalculateButtonPercentages();
    }
}
