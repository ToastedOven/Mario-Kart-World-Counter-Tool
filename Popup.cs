using Godot;
using System;

public partial class Popup : ColorRect
{
    public string text
    {
        get;
        set
        {
            field = value;
            content.Text = text;
        }
    }
    [Export] private Label content;
    [Export] private Button acceptButton;

    public override void _Ready()
    {
        acceptButton.Pressed += QueueFree;
    }
}
