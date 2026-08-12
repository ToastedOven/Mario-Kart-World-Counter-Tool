using Godot;
using System;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class TrackSelect : Control
{
    [Export] private Array<HistoryCardTrackPicker> cards = new();

    public override void _Ready()
    {
        foreach (var card in cards)
        {
            card.PressedButton += CardOnPressedButton;
        }
    }

    private void CardOnPressedButton(int trackId)
    {
        HistoryCardEditor.currentTrackPickerCard.trackId = trackId;
        HistoryCardEditor.currentTrackPickerCard.Texture = ButtonThing.buttons[trackId].Texture;
        if (HistoryCardEditor.currentTrackPickerCard == HistoryCardEditor.instance.ComingFrom)
        {
            HistoryCardEditor.currentTrackPickerCard.trackId %= 30;
        }
        else
        {
            HistoryCardEditor.currentTrackPickerCard.label.Text = ButtonThing.buttons[trackId].intermissionButton ? "Intermission" : "3Lap";
        }

        foreach (var card in cards)
        {
            card.HideStuff();
        }
        Visible = false;
    }
}
