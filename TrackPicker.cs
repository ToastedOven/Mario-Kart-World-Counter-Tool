using Godot;
using System;
using CounterTool;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class TrackPicker : HFlowContainer
{
    [Export] public Array<HistoryCardTrackPicker> trackCards = [];
    private HistoryCardTrackPicker currentCard;
    public static TrackPicker instance;
    public override void _Ready()
    {
        foreach (var card in trackCards)
        {
            card.PressedButton += CardOnPressedButton;
        }

        instance = this;
    }

    public override void _Process(double delta)
    {
        foreach (var card in trackCards)
        {
            if (card.mainButton.IsHovered())
            {
                if (currentCard != null && currentCard != card)
                {
                    currentCard.SetOptionsVisibility(false);
                }
                currentCard = card;
            }
        }
    }

    private void CardOnPressedButton(int trackId)
    {
        RecentTrackTracker.instance.AddTrack(trackId);
    }
}
