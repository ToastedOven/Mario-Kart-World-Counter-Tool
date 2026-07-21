using Godot;
using System;

public partial class RecentTrack : TextureRect
{
    public ButtonThing myTrack;
    [Export] private Button button;
    public override void _Ready()
    {
        button.Pressed += () => { RecentTrackTracker.instance.RemoveTrack(myTrack); };
    }
}
