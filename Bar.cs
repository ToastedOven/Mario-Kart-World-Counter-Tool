using Godot;
using System;
using System.Linq;
using Godot.Collections;

public partial class Bar : ColorRect
{
    [Export] private TextureRect texture;
    [Export] private Button button;
    [Export] private Label countLabel;
    public static Array<Bar> bars = new();
    private int spot;
    public static bool checkOffered = true;
    public override void _Ready()
    {
        base._Ready();
        spot = bars.Count;
        texture.Texture = ButtonThing.buttons[spot].Texture;
        bars.Add(this);
        ButtonThing.buttonForEvents.RecalculateNeeded += RecalculateSizes;
        button.MouseEntered += ButtonOnMouseEntered;
        button.MouseExited += ButtonOnMouseExited;
    }

    private void ButtonOnMouseExited()
    {
        PageSwitcher.instance.currentHoveredTrackInfo.Text = $"";
        PageSwitcher.instance.currentHoveredTrackTexture.Texture = null;
    }

    private void ButtonOnMouseEntered()
    {
        if (checkOffered)
        {
            PageSwitcher.instance.currentHoveredTrackInfo.Text = $"Total Offerings: {ButtonThing.buttons[spot].offeredValue + ButtonThing.buttons[spot + 30].offeredValue}";
        }
        else
        {
            PageSwitcher.instance.currentHoveredTrackInfo.Text = $"Total Picked: {ButtonThing.buttons[spot].pickedValue + ButtonThing.buttons[spot + 30].pickedValue}";
        }
        PageSwitcher.instance.currentHoveredTrackTexture.Texture = ButtonThing.buttons[spot].Texture;
    }

    private void RecalculateSizes()
    {
        if (checkOffered)
        {
            int myValue = ButtonThing.buttons[spot].offeredValue + ButtonThing.buttons[spot + 30].offeredValue;
            CustomMinimumSize = new Vector2(35,620f * (myValue / (float)ButtonThing.highestValue));
            countLabel.Text = myValue.ToString();   
        }
        else
        {
            int myValue = ButtonThing.buttons[spot].pickedValue + ButtonThing.buttons[spot + 30].pickedValue;
            CustomMinimumSize = new Vector2(35,620f * (myValue / (float)ButtonThing.highestPickedValue));
            countLabel.Text = myValue.ToString();
        }
        if (spot == 0)
        {
            SortBars();
        }
    }

    private async void SortBars()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (checkOffered)
        {
            var sortedBarOrder = bars.OrderByDescending(item => ButtonThing.buttons[item.spot].offeredValue + ButtonThing.buttons[item.spot + 30].offeredValue).ToArray();
            sortedBarOrder = sortedBarOrder.Reverse().ToArray();
            foreach (var bar in sortedBarOrder)
            {
                bar.GetParent().MoveChild(bar, 0);
            }   
        }
        else
        {
            var sortedBarOrder = bars.OrderByDescending(item => ButtonThing.buttons[item.spot].pickedValue + ButtonThing.buttons[item.spot + 30].pickedValue).ToArray();
            sortedBarOrder = sortedBarOrder.Reverse().ToArray();
            foreach (var bar in sortedBarOrder)
            {
                bar.GetParent().MoveChild(bar, 0);
            }
        }
    }
}
