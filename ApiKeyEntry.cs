using Godot;
using System;
using System.Text;

public partial class ApiKeyEntry : GenericPage
{
    [Export] private Button submit;
    [Export] private LineEdit textEntry;
    [Export] public Label responseLabel, dateLabel;
    public static string apiKey;
    public static string apiKeyDate;

    public override void _Ready()
    {
        base._Ready();
        submit.Pressed += () => { SubmitKey(textEntry.Text); };
        textEntry.TextSubmitted += SubmitKey;
    }

    public virtual async void SubmitKey(string text)
    {
        CloudflareClient client = new CloudflareClient(SettingsPage.dbUrl, "");
        string response = await client.CheckIfKeyIsValid(text);
        responseLabel.Text = response;
        if (response.Contains("valid\":true"))
        {
            apiKey = text;
            apiKeyDate = DateTime.Now.ToString("[MM/dd/yyyy h:mmtt]");
            dateLabel.Text = $"API Key Set Date: {apiKeyDate}";
            SettingsPage.Save();
        }
    }
}
