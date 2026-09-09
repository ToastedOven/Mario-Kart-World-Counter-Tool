using Godot;
using System;
using CounterTool;

public partial class HistoryCardEditor : ColorRect
{
    [Export] public HistoryCardTrackPicker Option1, Option2, Option3, Picked, ComingFrom;
    public static HistoryCardTrackPicker currentTrackPickerCard;
    [Export] private Button Combo;
    [Export] private TextureRect racer, kart;
    [Export] private CheckBox Random, Mirror, Disconnected, NewSession;
    [Export] private LineEdit Option1Votes, Option2Votes, Option3Votes, RandomVotes, Version, Position, PlayerCount;
    private HistoryCard currentCard;
    public static int racerIndex, kartIndex;
    public static HistoryCardEditor instance;
    private bool loaded;
    [Signal]
    public delegate void ItemInteractedEventHandler();
    public override void _Ready()
    {
        instance = this;
        Option1.openPicker = true;
        Option2.openPicker = true;
        Option3.openPicker = true;
        Picked.openPicker = true;
        ComingFrom.openPicker = true;
        Combo.Pressed += EmitSignalItemInteracted;
        Random.Pressed += EmitSignalItemInteracted;
        Mirror.Pressed += EmitSignalItemInteracted;
        Disconnected.Pressed += EmitSignalItemInteracted;
        NewSession.Pressed += EmitSignalItemInteracted;
        Option1Votes.FocusEntered += EmitSignalItemInteracted;
        Option2Votes.FocusEntered += EmitSignalItemInteracted;
        Option3Votes.FocusEntered += EmitSignalItemInteracted;
        RandomVotes.FocusEntered += EmitSignalItemInteracted;
        Version.FocusEntered += EmitSignalItemInteracted;
        Position.FocusEntered += EmitSignalItemInteracted;
        PlayerCount.FocusEntered += EmitSignalItemInteracted;
        Option1.PauseTimer += EmitSignalItemInteracted;
        Option2.PauseTimer += EmitSignalItemInteracted;
        Option3.PauseTimer += EmitSignalItemInteracted;
        Picked.PauseTimer += EmitSignalItemInteracted;
        ComingFrom.PauseTimer += EmitSignalItemInteracted;
    }

    public void Reload()
    {
        LoadCard(currentCard);
    }

    public void LoadCard(HistoryCard card)
    {
        if (card is null)
        {
            return;
        }
        currentCard = card;
        Option1.trackId = card.info["Option1"].ToInt();
        Option2.trackId = card.info["Option2"].ToInt();
        Option3.trackId = card.info["Option3"].ToInt();
        Picked.trackId = card.info["Picked"].ToInt();
        ComingFrom.trackId = card.info["ComingFrom"].ToInt();
        
        Option1.Texture = ControlManager.instance.trackTextures[Option1.trackId % ControlManager.TRACKCOUNT];
        Option2.Texture = ControlManager.instance.trackTextures[Option2.trackId % ControlManager.TRACKCOUNT];
        Option3.Texture = ControlManager.instance.trackTextures[Option3.trackId % ControlManager.TRACKCOUNT];
        Picked.Texture = ControlManager.instance.trackTextures[Picked.trackId % ControlManager.TRACKCOUNT];
        Option1.label.Text = Option1.trackId >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        Option2.label.Text = Option2.trackId >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        Option3.label.Text = Option3.trackId >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        Picked.label.Text = Picked.trackId >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        ComingFrom.Texture = ControlManager.instance.trackTextures[ComingFrom.trackId % ControlManager.TRACKCOUNT];
        ComingFrom.label.Text = "";
        Random.SetPressed(card.info["Random"] == "1");
        Mirror.SetPressed(card.info["Mirror"] == "1");
        Disconnected.SetPressed(card.info["Disconnected"] == "1");
        NewSession.SetPressed(card.info["NewSession"] == "1");
        Option1Votes.Text = card.info["Option1Votes"];
        Option2Votes.Text = card.info["Option2Votes"];
        Option3Votes.Text = card.info["Option3Votes"];
        RandomVotes.Text = card.info["RandomVotes"];
        Version.Text = card.info["Version"];
        Position.Text = card.info["Position"];
        PlayerCount.Text = card.info["PlayerCount"];
        if (!loaded)
        {
            racerIndex = card.info["DriverIndex"].ToInt();
            kartIndex = card.info["KartIndex"].ToInt();
        }
        racer.Texture = SearchBar.instance.characters.GetChild<TextureRect>(racerIndex).Texture;
        kart.Texture = SearchBar.instance.karts.GetChild<TextureRect>(kartIndex).Texture;
        if (!loaded)
        {
            loaded = true;
            Combo.Pressed += ComboOnPressed;
        }
    }

    private void ComboOnPressed()
    {
        SearchBar.instance.comboPage.Visible = true;
    }

    public void SaveCard()
    {
        if (currentCard is null)
        {
            return;
        }
        
        currentCard.info["Option1"] = Option1.trackId.ToString();
        currentCard.info["Option2"] = Option2.trackId.ToString();
        currentCard.info["Option3"] = Option3.trackId.ToString();
        currentCard.info["Picked"] = Picked.trackId.ToString();
        currentCard.info["ComingFrom"] = ComingFrom.trackId.ToString();
        currentCard.info["Random"] = Random.IsPressed() ? "1" : "0";
        currentCard.info["Mirror"] = Mirror.IsPressed() ? "1" : "0";
        currentCard.info["Disconnected"] = Disconnected.IsPressed() ? "1" : "0";
        currentCard.info["NewSession"] = NewSession.IsPressed() ? "1" : "0";
        currentCard.info["Option1Votes"] = Option1Votes.Text;
        currentCard.info["Option2Votes"] = Option2Votes.Text;
        currentCard.info["Option3Votes"] = Option3Votes.Text;
        currentCard.info["RandomVotes"] = RandomVotes.Text;
        currentCard.info["Version"] = Version.Text;
        if (!int.TryParse(Position.Text, out int number) || number < 1 || number > 24)
        {
            Position.Text = "-1";
        }
        if (!int.TryParse(PlayerCount.Text, out int number2) || number2 < 1 || number2 > 24)
        {
            PlayerCount.Text = "-1";
        }
        currentCard.info["Position"] = Position.Text;
        currentCard.info["PlayerCount"] = PlayerCount.Text;
        currentCard.info["DriverIndex"] = racerIndex.ToString();
        currentCard.info["KartIndex"] = kartIndex.ToString();
        
        currentCard = null;
    }
}
