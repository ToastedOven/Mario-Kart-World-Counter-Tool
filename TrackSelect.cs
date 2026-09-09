using Godot;
using System;
using CounterTool;
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
        HistoryCardEditor.currentTrackPickerCard.Texture = ControlManager.instance.trackTextures[trackId % ControlManager.TRACKCOUNT];
        if (HistoryCardEditor.currentTrackPickerCard == HistoryCardEditor.instance.ComingFrom)
        {
            HistoryCardEditor.currentTrackPickerCard.trackId %= ControlManager.TRACKCOUNT;
        }
        else
        {
            HistoryCardEditor.currentTrackPickerCard.label.Text = trackId >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        }

        foreach (var card in cards)
        {
            card.HideStuff();
        }
        Visible = false;
    }
}
