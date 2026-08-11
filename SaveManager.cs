using System.Text;
using Godot;

namespace CounterTool;

public class SaveManager
{
    public static void Save()
    {
        StringBuilder saveInfo = new StringBuilder();
        foreach (var button in ButtonThing.buttons)
        {
            saveInfo.Append($"{button.offeredValue} ");
        }
        foreach (var button in ButtonThing.buttons)
        {
            saveInfo.Append($"{button.pickedValue} ");
        }

        saveInfo.Append("\n");
        foreach (var historyCard in HistoryHandler.cards)
        {
            saveInfo.Append($"{historyCard.GetDataForSaving()}|");
        }
        var saveFile = FileAccess.Open("user://marioKartCourseTracker.save", FileAccess.ModeFlags.Write);
        saveFile.StoreString(saveInfo.ToString());
        saveFile.Close();
    }

    public static void Load()
    {
        for (int i = 30; i < 60; i++)
        {
            ButtonThing.buttons[i].intermissionButton = true;
        }
        var saveFile = FileAccess.Open("user://marioKartCourseTracker.save", FileAccess.ModeFlags.Read);
        if (saveFile is not null)
        {
            var fileContents = saveFile.GetAsText();
            var numbers = fileContents.Split(" ");
            foreach (var button in ButtonThing.buttons)
            {
                button.SetOfferedValue(int.Parse(numbers[ButtonThing.buttons.IndexOf(button)]), false);
                button.SetPickedValue(int.Parse(numbers[ButtonThing.buttons.IndexOf(button) + 60]), false);
            }

            var historyInfo = fileContents.Split("\n")[1];
            HistoryHandler.instance.LoadAllCards(historyInfo);
            
            saveFile.Close();
        }
        else
        {
            foreach (var button in ButtonThing.buttons)
            {
                button.SetOfferedValue(0, false);
            }
        }
    }
}