using Godot;
using System;

public partial class RecentTrack : TextureRect
{
    public ButtonThing myTrack
    {
        get;
        set
        {
            field = value;
            Texture = field.Texture;
            GetNode<Label>("Label").Text = field.intermissionButton ? "Intermission" : "3Lap";
        }
    }
    [Export] private Button button;
    
    public override void _Ready()
    {
        button.GuiInput += ButtonOnGuiInput;

        void ButtonOnGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true } mouseEvent) //wtf is this pattern, Rider suggested it and it looks weird, but I'm leaving it.
            {
                switch (mouseEvent.ButtonIndex)
                {
                    case MouseButton.Left:
                        myTrack = RecentTrackTracker.instance.SwapTrack(myTrack, ButtonThing.buttons[(ButtonThing.buttons.IndexOf(myTrack) + 30) % 60]);
                        break;

                    case MouseButton.Right:
                        RecentTrackTracker.instance.RemoveTrack(myTrack);
                        break;
                }
            }
        }
    }
}
