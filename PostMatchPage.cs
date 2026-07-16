using Godot;
using System;

public partial class PostMatchPage : Control
{
    public static PostMatchPage  instance;
    [Export] public TextureRect track, driver, kart;
    public override void _Ready()
    {
        instance = this;
    }
}
