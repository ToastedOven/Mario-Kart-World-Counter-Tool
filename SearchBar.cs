using Godot;
using System;

public partial class SearchBar : LineEdit
{
    public static SearchBar instance;
    [Export]
    public Label threeLapsTotalLabel, intermissionsTotalLabel;

    [Export] private Button comboChanger;
    [Export] private Control pickerPage, comboPage;
    [Export] private TextureRect currentKart, currentCharacter;
    [Export] public GridContainer karts, characters, positions;
    bool hidingStats;
    public override void _Ready()
    {
        TextChanged += OnTextChanged;
        instance = this;
        HideStatsButtonOnPressed();
        comboChanger.Pressed += ComboChangerOnPressed;
    }

    private void ComboChangerOnPressed()
    {
        pickerPage.Visible = false;
        comboPage.Visible = true;
    }

    private async void HideStatsButtonOnPressed()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        hidingStats = !hidingStats;
        foreach (var button in ButtonThing.buttons)
        {
            button.text.Visible = !hidingStats;
            button.allPercentage.Visible = !hidingStats;
            button.categoryPercentage.Visible = !hidingStats;
        }
    }

    private void OnTextChanged(string newText)
    {
        foreach (var button in ButtonThing.buttons)
        {
            if (button.Name.ToString().ToLowerInvariant().Contains(newText.ToLowerInvariant()))
            {
                button.SetDimmed(false);
            }
            else
            {
                button.SetDimmed(true);
            }
        }
    }

    public void FinishComboPicking()
    {
        currentKart.Texture = karts.GetChild<TextureRect>(ComboButton.currentKart).Texture;
        currentCharacter.Texture = characters.GetChild<TextureRect>(ComboButton.currentDriver).Texture;
        comboPage.Visible = false;
        pickerPage.Visible = true;
    }
}
