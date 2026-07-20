using Godot;
using System;
using System.Text;

public partial class ApiKeyEntry : GenericPage
{
    [Export] private Button submit;
    [Export] private LineEdit textEntry;
    [Export] private Label responseLabel, dateLabel;
    public static string apiKey;
    private string apiKeyDate;

    public override void _Ready()
    {
        base._Ready();
        submit.Pressed += () => { SubmitKey(textEntry.Text); };
        textEntry.TextSubmitted += SubmitKey;
        Load();
    }

    private async void SubmitKey(string text)
    {
        CloudflareClient client = new CloudflareClient("https://nunchuk-db-proxy.dwelxs2.workers.dev/", "");
        string response = await client.CheckIfKeyIsValid(text);
        responseLabel.Text = response;
        if (response.Contains("valid\":true"))
        {
            apiKey = text;
            apiKeyDate = DateTime.Now.ToString("[MM/dd/yyyy h:mmtt");
            dateLabel.Text = $"API Key Set Date: {apiKeyDate}";
            Save();
        }
    }
    private void Save()
    {
        StringBuilder saveInfo = new StringBuilder();
        var saveFile = FileAccess.Open("user://MkctApi.settings", FileAccess.ModeFlags.Write);
        saveInfo.Append($"{apiKey}\n{apiKeyDate}");
        saveFile.StoreString(saveInfo.ToString());
        saveFile.Close();
    }

    private void Load()
    {
        var saveFile = FileAccess.Open("user://MkctApi.settings", FileAccess.ModeFlags.Read);
        if (saveFile is not null)
        {
            var fileContents = saveFile.GetAsText();
            apiKey = fileContents.Split("\n")[0];
            apiKeyDate = fileContents.Split("\n")[1];
            dateLabel.Text = $"API Key Set Date: {apiKeyDate}";
            saveFile.Close();
        }
    }
}
