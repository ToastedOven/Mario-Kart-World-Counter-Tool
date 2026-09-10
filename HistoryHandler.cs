using Godot;
using System;
using System.Linq;
using Godot.Collections;

public partial class HistoryHandler : HBoxContainer
{
    [Export] PackedScene cardScene;
    public static Array<HistoryCard> cards = new();
    public static HistoryHandler instance;

    public override void _Ready()
    {
        instance = this;
    }

    public void LoadAllCards(string cardData)
    {
        var splitData = cardData.Split("|");
        foreach (var data in splitData)
        {
            if (data.Contains(","))
            {
                CreateCard(data);
            }
        }
    }

    public HistoryCard CreateCard(string cardData)
    {
        var newCard = cardScene.Instantiate<HistoryCard>();
        AddChild(newCard);
        MoveChild(newCard, 0);
        newCard.Setup(cardData);
        cards.Add(newCard);
        foreach (var card in TrackPicker.instance.trackCards)
        {
            card.intermissionBool = false;
        }
        return newCard;
    }

    public void RemoveLatestCard()
    {
        cards.Last().QueueFree();
        cards.Remove(cards.Last());
    }
}
