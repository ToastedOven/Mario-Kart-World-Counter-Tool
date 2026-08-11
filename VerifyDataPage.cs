using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CounterTool;

public partial class VerifyDataPage : Control
{
    [Export] private GridContainer historyDataContainer;
    [Export] Button confirmButton, switchViewButton;
    [Export] PackedScene historyDataScene;
    [Export] private HistoryCardEditor editor; 
    private static List<HistoryInfo> dataSets = new();
    private double autoConfirmTimer;
    public static VerifyDataPage instance;
    private static HistoryCard currentCard;

    public override void _Ready()
    {
        confirmButton.Pressed += ConfirmButtonOnPressed;
        instance = this;
        switchViewButton.Pressed += SwitchViewButtonOnPressed;
        editor.ItemInteracted += () => autoConfirming = false;
    }

    private void SwitchViewButtonOnPressed()
    {
        autoConfirming = false;
        historyDataContainer.Visible = !historyDataContainer.Visible; 
        editor.Visible = !historyDataContainer.Visible;
        if (editor.Visible)
        {
            SaveRawData();
            editor.LoadCard(currentCard);
            switchViewButton.Text = "Raw View";
        }
        else
        {
            editor.SaveCard();
            LoadHistoryInfo(currentCard);
            switchViewButton.Text = "Editor View";
        }
    }

    public override void _Process(double delta)
    {
        if (autoConfirmTimer > 0)
        {
            if (!autoConfirming)
            {
                autoConfirmTimer = 0;
                return;
            }
            autoConfirmTimer -= delta;
            confirmButton.Text = $"Confirming in {(int)autoConfirmTimer}";
            if (autoConfirmTimer <= 0)
            {
                ConfirmButtonOnPressed();
            }
        }
    }

    private async void ConfirmButtonOnPressed()
    {
        try
        {
            autoConfirmTimer = 0;
        
            SaveCard();
            Visible = false;
        
            await FinishHistory();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void SaveCard()
    {
        if (editor.Visible)
        {
            editor.SaveCard();
        }
        else
        {
            SaveRawData();
        }
    }

    private static void SaveRawData()
    {
        foreach (var data in dataSets)
        {
            currentCard.info[data.name.Text] = data.data.Text;
            data.QueueFree();
        }

        dataSets.Clear();
    }

    private bool autoConfirming
    {
        get;
        set
        {
            field = value;
            if (!field)
            {
                confirmButton.Text = "Confirm";
            }
        }
    } = true;

    private static bool editing;
    public void SetupHistoryCard(HistoryCard card)
    {
        Visible = true;
        editing = true;
        autoConfirming = false;
        currentCard = card;
        editor.LoadCard(currentCard);
        LoadHistoryInfo(currentCard);
    }
    public void SetupHistoryCard(string matchInfo)
    {
        Visible = true;
        autoConfirmTimer = 30;
        autoConfirming = true;
        currentCard = HistoryHandler.instance.CreateCard(matchInfo);
        editor.LoadCard(currentCard);
        LoadHistoryInfo(currentCard);
    }

    private void LoadHistoryInfo(HistoryCard card)
    {
        foreach (var thing in dataSets)
        {
            thing.QueueFree();
        }
        dataSets.Clear();
        foreach (var kvp in card.info)
        {
            HistoryInfo info = historyDataScene.Instantiate<HistoryInfo>();
            info.name.Text = kvp.Key;
            info.data.Text = kvp.Value;
            historyDataContainer.AddChild(info);
            dataSets.Add(info);
            info.data.TextChanged += text => { autoConfirming = false; };
            info.data.FocusEntered += () => {  autoConfirming = false;  };
        }
    }
    
    private static async Task FinishHistory()
    {
        var newMatch = currentCard.GetHistoryInfoForDb();
            
        if (SettingsPage.autoUpload && !editing)
        {
            var d1Client = new CloudflareClient(SettingsPage.dbUrl, ApiKeyEntry.apiKey);
            var (worked, output) = await d1Client.InsertHistoryEntryAsync(newMatch);
            if (worked)
            {
                HistoryHandler.instance.RemoveLatestCard();
            }
            else
            {
                ControlManager.instance.CreatePopup(output);
            }
        }
        else if (editing)
        {
            instance.SaveCard();
            currentCard.ReSetup();
        }

        editing = false;
        SaveManager.Save();
        PositionButton.currentRacePosition = -1;
    }
}
