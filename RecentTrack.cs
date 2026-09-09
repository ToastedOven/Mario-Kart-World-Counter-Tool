using Godot;
using System;
using CounterTool;

public partial class RecentTrack : TextureRect
{
    public HistoryCardTrackPicker myTrack
    {
        get;
        set
        {
            field = value;
            if (field is not null)
            {
                Texture = field.Texture;
                GetNode<Label>("Label").Text = field.intermissionBool ? "Intermission" : "3Lap";
            }
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
                        myTrack.intermissionBool = !myTrack.intermissionBool;
                        myTrack = RecentTrackTracker.instance.SwapTrack(myTrack, myTrack);
                        break;

                    case MouseButton.Right:
                        RecentTrackTracker.instance.RemoveTrack(myTrack);
                        break;
                }
            }
        }
    }
}
