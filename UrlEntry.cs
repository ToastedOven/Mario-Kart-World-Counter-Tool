using Godot;
using System;
using System.Text;

public partial class UrlEntry : GenericPage
{
    [Export] private Button submit;
    [Export] private LineEdit textEntry;
    [Export] public Label urlLabel;

    public override void _Ready()
    {
        base._Ready();
        submit.Pressed += () => { SubmitKey(textEntry.Text); };
        textEntry.TextSubmitted += SubmitKey;
        urlLabel.Text = $"Database URL: {SettingsPage.dbUrl}";
    }

    public virtual void SubmitKey(string text)
    {
        SettingsPage.dbUrl = text?.Trim() ?? string.Empty;
        urlLabel.Text = $"Database URL: {SettingsPage.dbUrl}";
        SettingsPage.Save();
    }
}
