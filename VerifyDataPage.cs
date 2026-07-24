using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CounterTool;

public partial class VerifyDataPage : Control
{
    [Export] private GridContainer historyDataContainer;
    [Export] Button confirmButton;
    [Export] PackedScene historyDataScene;
    private static List<HistoryInfo> dataSets = new();
    string currentHistory = "";
    private double autoConfirmTimer;
    public static VerifyDataPage instance;

    public override void _Ready()
    {
        confirmButton.Pressed += ConfirmButtonOnPressed;
        instance = this;
    }

    public override void _Process(double delta)
    {
        if (autoConfirmTimer > 0)
        {
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
        autoConfirmTimer = 0;
        StringBuilder sb = new();
        foreach (var data in dataSets)
        {
            sb.Append($"{data.name.Text}>>{data.data.Text},");
        }
        sb.Remove(sb.Length - 1, 1);
        currentHistory = sb.ToString();
        Visible = false;
        foreach (var thing in dataSets)
        {
            thing.QueueFree();
        }
        dataSets.Clear();
        await FinishHistory(currentHistory);
    }

    public void LoadHistoryInfo(string history)
    {
        Visible = true;
        autoConfirmTimer = 30;
        currentHistory = history;
        foreach (var dataPoint in currentHistory.Split(","))
        {
            HistoryInfo info = historyDataScene.Instantiate<HistoryInfo>();
            info.name.Text = dataPoint.Split(">>")[0];
            info.data.Text = dataPoint.Split(">>")[1];
            historyDataContainer.AddChild(info);
            dataSets.Add(info);
            info.data.TextChanged += text => { autoConfirmTimer = 30; };
        }
    }
    
    private static async Task FinishHistory(string matchInfo)
    {
        var historyCard = HistoryHandler.instance.CreateCard(matchInfo);
        var newMatch = historyCard.GetHistoryInfoForDb();
            
        if (SettingsPage.autoUpload)
        {
            var d1Client = new CloudflareClient(SettingsPage.dbUrl, ApiKeyEntry.apiKey);
            await d1Client.InsertHistoryEntryAsync(newMatch);   
            HistoryHandler.instance.RemoveLatestCard();
        }
        SaveManager.Save();
        PositionButton.currentRacePosition = -1;
    }
}
