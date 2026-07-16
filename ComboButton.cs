using Godot;
using System;

public partial class ComboButton : TextureRect
{
    [Export] private Button button;
    private int positionInList;
    public static int currentKart, currentDriver;
    [Export]
    private bool kartButton;
    public override void _Ready()
    {
        positionInList = GetIndex();
        button.Pressed += ButtonOnPressed;
    }

    private void ButtonOnPressed()
    {
        if (kartButton)
        {
            currentKart = positionInList;
            SearchBar.instance.karts.Visible = false;
            SearchBar.instance.characters.Visible = true;
            SearchBar.instance.FinishComboPicking();
        }
        else
        {
            currentDriver = positionInList;
            SearchBar.instance.karts.Visible = true;
            SearchBar.instance.characters.Visible = false;
        }
    }
}
