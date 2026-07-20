using Godot;
using System;

public partial class GenericPage : Control
{
    [Export] private Button close;

    public override void _Ready()
    {
        close.Pressed += () => { this.Visible = false; };
    }
}
